---
name: primordia-boss-lepidodendron
description: "SIGUIENTE (tanda 6, último jefe del piso 3): arte del jefe secreto Lepidodendron ancestral; diseño cerrado, qué reutilizar y qué preguntar antes de dibujar"
metadata:
  node_type: memory
  type: project
  originSessionId: 279cdbc0-773e-407f-aa8d-202f5398ba38
  modified: 2026-11-01T21:53:40.245Z
---

Jefe secreto del piso 3 ([[primordia-floor3-design]], líneas «Jefe secreto Lepidodendron ancestral» y «Recompensa del jefe secreto», cerradas el 2026-10-28). Es lo SIGUIENTE tras la Megarachne ([[primordia-boss-megarachne]], terminada el 2026-11-01). Con Opus. Proceso: [[primordia-boss-art-workflow]] (preguntas antes de cada subtanda, muestra en 1 dirección, escenas con GIF en `gif/escenas/`, revisar yo cada lámina antes de enseñarla, README + MULTIMEDIA.md + commit al cerrar).

**Diseño (resumen):** sala horizontal muy larga (4-5 pantallas), el árbol al fondo desde el inicio. 3 paredes de raíces = 25 % de vida cada una (dañar pared = dañar jefe; golpes o mucho más rápido con fuego: odres y helechos junto a cada pared; nudos débiles ×2); oleadas por pared (1 Calamites y sapos; 2 arañas y bombarderos, cuyo cono prende la pared; 3 mezcla + libélulas); esporas en arco por encima de las paredes. Fase final 25 %: tronco −80 % de daño, 3 conos (estróbilos) que se abren tras cada patrón y reciben daño completo; latigazos de raíces desde los lados (aviso), bullet hell sencillo de esporas desde los conos, enemigos desde las esquinas, raíces que brotan bajo el jugador, nubes de metano que prendidas junto al tronco abren todos los conos. Muerte: **desplome del árbol** como cinemática y última amenaza (esquivar la sombra, nube de esporas). Recompensa: +1 corazón permanente, **Espora morada** (llave del piso 4), reliquia **Raíz ancestral** en un **pedestal dorado** en una cámara tras el árbol (animación especial al cogerla). Morado = camino secreto.

**Reutilizable:** el Lepidodendron del Nido (`floor3-bosses/megarachne/generar_nido_megarachne.py`: `tree(mode)` con copa en cúpula, `bark_trunk` de corteza en rombos, estados empapado/seco/carbonizado, `Nido_Burn` para fuego que recorre); conos-cofre/estróbilos (tanda 1c, `floor3-environment/generar_piezas_piso3.py`); muro de raíces de la sala secreta y puertas de raíces (tanda 1a/1c, `root()` en base_piso3); esporas de Calamites (tanda 3a); motor 3D (`arthropleura/generar_arthropleura.py`) si hace falta volumen; presentaciones (`boss-intros/generar_presentaciones_piso3.py`: añadir retrato + rótulo ámbar; ¿sello distinto para jefe secreto?, preguntar).

**Preguntar antes de dibujar (ronda 1):** vista del árbol (¿frontal al fondo como pared o picada?), tamaño (¿cuántas pantallas de alto?), aspecto (ancestral: ¿más viejo, morado secreto en vetas?), las 3 paredes de raíces (forma, estados de daño/fuego), los 3 conos (dónde, abrir/cerrar), ataques (latigazos, raíces bajo el jugador, esporas), desplome (cinemática), barra de vida (¿4 tramos por pared + tronco?), presentación, reliquia y pedestal.
