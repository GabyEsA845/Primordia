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

**Actualizacion 2026-11-13 (Lepidodendron):** L1 y L2 fusionadas; L3 (tarea 07) hecha por Codex en `codex/lepido-3-conos` SIN fusionar ni probar. Escritas y pendientes de Codex, apiladas en cadena (cada rama sale de la anterior, la usuaria prueba todo al final y luego se fusiona en orden): 08 ataques (`codex/lepido-4-ataques`), 09 barra+presentacion+desplome (`codex/lepido-5a-desplome`), 10 recompensa+tocon+reliquia (`codex/lepido-5b-recompensa`). Numeros de L4 cerrados (7/9/12/20 s, -15 % por cono). El enganche a Floor3.asset/puerta secreta queda fuera. Plan: `docs/codex/lepidodendron-plan.md`.

**2026-11-13 (tarde):** Codex se quedo sin tokens: las tareas 09 (barra/desplome) y 10 (recompensa/tocon/reliquia) las termino y rellena Claude. Ramas en cadena sin fusionar: `codex/lepido-3-conos` -> `4-ataques` -> `5a-desplome` -> `5b-recompensa`.

**2026-11-14: Codex RETOMADO (la usuaria tiene tokens otra vez).** Arthropleura: base de las ramas = `feature/boss-arthropleura` (no feature/floor-3). Tarea 11 (B2: fuego, placas abiertas, daño al jefe, 70 %) escrita en `docs/codex/tarea-11-arthropleura-b2-fuego.md`, rama `codex/arthro-b2-fuego` creada; pendiente de que Codex la haga. Decidido con la usuaria: el cuerpo solo se prende dentro del claro (odre a ≤2,5 u o fuego del jugador; mecha 0,4 s/pieza, contagio sí cruza la espesura); decorado con arte ya aprobado.
**2026-11-14 (bloqueo de la tarea 11):** Codex paró bien: con tope Burn 2/s y piezas Charred para siempre una quemada hacía ~36 PV y F1 pide 180 (F2 210). DECIDIDO con la usuaria: Charred se enfría ~25 s y se puede volver a prender, y el tope Burn de la Arthropleura sube a 6 PV/s (una quemada ≈ 90-110 PV; F1 ≈ 2 quemadas). Leccion: al escribir tareas de daño, multiplicar duración × tope antes de pasarla a Codex.

**2026-11-14 (tarea 12):** B2 probada por la usuaria (sin commit aun). Presentacion de entrada de la Arthropleura = tarea 12 (`docs/codex/tarea-12-arthropleura-presentacion.md`, rama `codex/arthro-intro`): dormido, al llegar al centro temblor + bulto bajo la espesura entra por un lado de la camara, ~3/4 de vuelta, agujero arriba con la cabeza rugiendo, BossIntro (retrato «Coloso del Carbonifero»), 1.a embestida desde ese agujero. Sustituye mi presentacion provisional (WakeUp/StepIntro).

**2026-11-14 (tarde):** Codex se quedo sin tokens a mitad de la tarea 12; Claude la termino (retoque de 1.a embestida, prueba corregida) y rellena el Resultado. 168/168 pruebas de jefes en verde. Falta que la usuaria pruebe la presentacion en Unity; hasta entonces NO hay commit de B2 ni de los ajustes (rama `codex/arthro-intro`).

**2026-11-15 (Megarachne):** Arthropleura F en commit c65e24f1 (rama feature/boss-arthropleura). Megarachne: plan `docs/codex/megarachne-plan.md`, rama `feature/boss-megarachne` (desde la de la Arthropleura), tarea 13 (M1 base) en `codex/mega-1-base` (el plan y la tarea estan commiteados en esa rama de Codex, 21769998). Recompensa decidida: corazon + biomasa + tarjetas para subir 1 nivel una mutacion equipada que no sea nivel 3. Vida 150; dano salto 2, red 1, crias 1, seda 1.
