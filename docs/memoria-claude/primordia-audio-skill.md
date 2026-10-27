---
name: primordia-audio-skill
description: "Skill propia primordia-audio (2026-10-27) para sintetizar música y sonidos por código; aprobada y en main desde 2a6268f; identidad del piso 2 cerrada en pisos.md"
metadata:
  node_type: memory
  type: project
  originSessionId: dc1d32ca-4c5b-4c78-a785-2f47937ada22
  modified: 2026-10-27T14:00:59.286Z
---

El 2026-10-27, antes de empezar el audio del piso 2, el usuario aprobó crear una skill de audio como la de sprites ([[primordia-sprites-skill]]).

Hecha en `.claude/skills/primordia-audio/` (rama `feature/floor2-audio`, aprobada, commit ae4b1f5): SKILL.md (piezas de docs/multimedia-temp/audio, reglas fijas, flujo con aprobación de la lista de sonidos antes de sintetizar), references/pisos.md (identidad sonora: piso 1 completo; piso 2 con lo decidido y las preguntas abiertas: tonalidad, música por franja, capas, instrumentos), references/tecnica.md (capas ataque/cuerpo/cola, bandas por familia, bucles, BPM con pulso entero, trampas), references/plantilla_piso.py (efectos_pisoN.py + musica_pisoN.py que registran en los mismos SFX/MUSIC; probada) y scripts/lint_audio.py (formato, pico, silencios, chasquidos, empalmes, variantes, capas, sonidos.json frente a Sounds.cs y "Music_…" del código; `--solo`, `--tabla`, `--espectros` para mirar espectrogramas con Read, porque no puedo escuchar).

Decisión: los pisos nuevos van en módulos aparte (efectos_piso2.py, musica_piso2.py, PROMPT_piso2.md) en la misma carpeta, porque AudioSetup lee un único sonidos.json de docs/multimedia-temp/audio; generar_audio.py necesitará importar esos módulos y leer todos los PROMPT*.md.

Hallazgos al probarla: el Vorbis deja 2 muestras a 0 dBFS en Music_Boss_Amoeba_Final (no se oye, no se regenera); `finish_sfx` recorta el arranque silencioso y acorta los avisos que crecen desde cero; `lp` no admite corte variable (usar `svf`).

**How to apply:** usar la skill en todo audio nuevo; el piso 2 ya está cerrado en references/pisos.md ([[primordia-floor2-audio]]); para un piso nuevo, proponer su identidad sonora y preguntar antes de sintetizar.
