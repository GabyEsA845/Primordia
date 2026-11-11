---
name: primordia-codex-paused
description: Flujo Claude dirige + Codex implementa (RETOMADO 2026-11-11): reglas, archivos, tareas hechas y leccion aprendida
metadata:
  type: project
---

El 2026-10-31 se archivo trabajar con Codex; el 2026-11-11 la usuaria lo **retomo** (ChatGPT Plus gratis 1 mes, Codex instalado en el proyecto; hasta ~2026-12-11).

**Flujo:** Claude dirige (diseno, preguntas, revision del diff, memoria) y escribe cada tarea en `docs/codex/tarea-NN-*.md` (plantilla `docs/codex/PLANTILLA.md`); la usuaria le dice a Codex «Lee `AGENTS.md` y haz `docs/codex/tarea-NN-...`»; Codex trabaja en una rama `codex/<nombre>` desde `feature/floor-3`, sin commit/push/merge, y rellena `## Resultado` al final de la tarea. Claude revisa el diff, la usuaria prueba en Unity (`Test_BossMeganeura` etc.) y confirma; entonces Claude hace commit en la rama de Codex y la fusiona (`--no-ff`) en `feature/floor-3`, y crea la rama de la tarea siguiente.
**Reglas para Codex:** `AGENTS.md` en la raiz (cambios de editor solo por menus `Primordia`, **el arte y el audio siempre llegan hechos: solo importar y programar**, no ejecutar los generadores `.py`, pregunta antes de lanzar Unity; la usuaria dio **autorizacion permanente** para lanzar Unity en batchmode con logs/XML fuera del repo y sin commit). Modelo recomendado en Codex: el mas capaz de programacion con esfuerzo medio/alto (no «mini»).
**Tareas hechas (todas fusionadas en feature/floor-3):** 01 barra de la Meganeura, 02 muerte, 03 pulido de la tormenta. **Escrita y pendiente de dar a Codex:** 04 telarana ligera.

**Why:** ahorrar el limite de uso de Claude (revisar sale mas barato que implementar); el mes gratis de Codex.
**How to apply:** solo una instancia de Unity a la vez; no edito `Assets/` mientras Codex trabaja en el mismo arbol (comparten carpeta y rama activa); al escribir una tarea, no mandar a Codex lo que requiera decisiones de diseno sin cerrar (preguntar a la usuaria antes) ni arte nuevo (Opus). Cuidado con mis propias instrucciones: un error de la tarea (p. ej. «inmune sin ojos») lo implementa Codex a la letra. Sigue valiendo [[primordia-token-saving]]. En un ordenador nuevo: Codex vera `AGENTS.md`; la ruta de Unity y del proyecto es la de ese equipo.
