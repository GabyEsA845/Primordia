---
name: primordia-lineage-evolution
description: Diseño aprobado 2026-10-15 de la evolución visual al completar un linaje (animación + piezas que cambian); rama feature/lineage-evolution
metadata:
  type: project
---

Idea del usuario (2026-10-15): al completar un linaje, el jugador "evoluciona" al animal. Descartado el cuerpo completo por linaje (77+ hojas, choca con la forma por piso × familia de arma y con las piezas; ver [[primordia-floors-design]], [[primordia-evolving-body]]). Aprobado A + B con sustitución:

- **Momento de evolución:** pausa de unos 2 s con el juego congelado (sin daño) y un pequeño zoom de cámara; la célula brilla, se transforma en la silueta del animal, sale su nombre y vuelve con las piezas nuevas. Si el linaje se completa con un menú abierto (elección 1 de 3, tienda), la animación espera a volver al juego. Una hoja por linaje (7).
- **Piezas que cambian mientras el linaje está activo** (sustituyen el aspecto de su ranura, misma mecánica; la Piel nunca; iconos del HUD/pausa sin cambios): Tiburón Extremidades → aleta dorsal + pectorales; Calamar Extremidades → tentáculos; Tortuga Boca → pico córneo y Extremidades → patas escamosas; Medusa Extremidades → tentáculos translúcidos colgando; Escorpión Boca → cola curvada sobre el lomo (dispara desde su punta) y Extremidades → patas con pinzas; Moho Extremidades → pelusa de moho; Cuesco de lobo Boca → gorrito de cuesco (bola blanca que suelta polvo).
- **Al perder el linaje:** pof pequeño del color de la clase y vuelven las piezas normales al instante.

**Why:** completar un linaje necesitaba un momento visible, pero sin multiplicar el arte ni recargar al jugador (el usuario pidió quitar piezas en vez de sumarlas).
**How to apply:** el arte lo genero yo con script Python y vista previa antes de importar; seguir [[primordia-workflow]].

**Estado 2026-10-15 (en main: commit b1531a8, merge bb1560d, confirmado por el usuario):** arte aprobado por el usuario (`docs/multimedia-temp/lineage-evolution/generar_evolucion_linajes.py`) y código: `GameState.Evolution` (congela el tiempo), `LineageData.ConfigureLook` (mouthLook, mouthMuzzleDistance, limbsLook, evolutionEffect), `MutationLook.SetLineageLook/Hidden/LookOf`, `LineageEvolution` (espera a Playing, oculta al jugador, zoom desactivando Pixel Perfect durante la animación, pof al perder), `LineageBanner` sale con `Evolving`, retrato de la pausa con piezas del linaje. Menú Primordia > Art > Importar evolución de linajes. Pruebas en `LineageEvolutionTests`.
