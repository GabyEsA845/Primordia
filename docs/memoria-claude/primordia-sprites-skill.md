---
name: primordia-sprites-skill
description: "Skill propia primordia-sprites (2026-10-26) para hacer el pixel art por código; rama feature/primordia-sprites (sale de feature/floor2-mutations), sin commit, esperando que el usuario la revise"
metadata:
  node_type: memory
  type: project
  originSessionId: bc20fcc8-a95a-48cd-a40d-3f24382d122a
  modified: 2026-10-26T20:40:58.389Z
---

El 2026-10-26 el usuario priorizó crear una skill propia de sprites en vez de instalar el plugin de Unity, skills genéricas de pixel art o Caveman/Ponytail (los descartamos: el plugin de Unity choca con el ciclo de menús de editor y confirmación; Caveman y Ponytail ahorran poco y empeoran explicaciones y pruebas).

Hecha en `.claude/skills/primordia-sprites/` (viaja con git a los dos equipos): SKILL.md (reglas fijas y flujo), references/pisos.md (tamaños y paletas por piso; completar al diseñar el piso 3), references/tecnica.md (reglas sacadas de Pixel Art Studio/Professional y MCP de Aseprite + lista de comprobación), references/plantilla_generador.py, scripts/pixel.py (misma API que floor2-bosses/base_piso2.py + `ramp()` con el tono desplazado + `shade()` sin sombreado de almohada; `shade_legacy` para regenerar arte aprobado) y scripts/lint_sprites.py (dibujo cortado, huérfanos, contorno grueso, colores; `--marcas` y `--celda`). También se corrigió en docs/MULTIMEDIA.md la frase de que multimedia-temp no se sube.

Rama `feature/primordia-sprites` sacada de `feature/floor2-mutations` (multimedia-temp aún no está en main). Aprobada y fusionada en feature/floor2-mutations (merge 6fb8ff2) y con ella en main (d5680b5, 2026-10-26); primer uso: el arte de la Carroña.

**Why:** que el arte nuevo salga coherente y con mejor técnica sin releer generadores antiguos.
**How to apply:** usar la skill en todo arte nuevo; no regenerar arte aprobado sin que lo pida. Ver [[primordia-workflow]], [[primordia-multimedia-temp]], [[primordia-floor2-mutations]].
