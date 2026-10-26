---
name: primordia-evolving-body
description: "Cuerpo evolutivo combinado (formas por parejas de temáticas + rasgos por capas): DESCARTADO el 2026-10-13 por exceso de arte; código guardado en rama y stash, no integrar"
metadata:
  node_type: memory
  type: project
  originSessionId: 5bb05669-19a4-44cc-90b6-804910fd3b05
  modified: 2026-10-14T02:52:37.700Z
---

**Descartado (2026-10-13).** El usuario decidió que el sistema de formas por parejas de temáticas × 3 versiones + rasgos por nivel saturaba el trabajo de sprites. Lo sustituye el diseño de [[primordia-floors-design]] (criatura por piso × familia de arma).

**Dónde quedó (por si se quiere recuperar algo, p. ej. el script generador o el ataque del Aguijón con preparación):**
- Rama `feature/evolving-body`, commit 33dfd9a (piloto del Aguijón: avispa larvaria y luciérnaga), sin push ni merge. No borrada.
- `git stash` "Hormiga (01) sin probar, cuerpo evolutivo combinado": forma Ant importada y lista `EvolutionThemes` en `ArtSetup.EvolvingBody.cs`.
- Script `docs/multimedia-temp/evolving-body/generar_cuerpo_evolutivo.py` (carpeta ignorada por git) y encargos `prompts/01-06` que ya no se usan.

El usuario volvió a `main` (ccba8b0). No preguntar por los encargos 02-06.
