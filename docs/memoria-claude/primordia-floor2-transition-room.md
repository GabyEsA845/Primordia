---
name: primordia-floor2-transition-room
description: "Sala de paso del piso 2 al 3: diseño decidido y arte/audio HECHOS (2026-11-03); falta programarla con Sonnet y luego la metamorfosis por pareja"
metadata:
  node_type: memory
  type: project
  originSessionId: f5d55923-15b7-4dfa-92ca-cfc308b7abdd
  modified: 2026-11-03T04:24:39.907Z
---

Adelantada del paso 6 del piso 3 (la usuaria lo decidió el 2026-11-03): sin ella la metamorfosis del piso 2 al 3 no se
puede ver en partida (el piso 2 da la victoria: `Floor2Setup` llama `ConfigureTransition(null, …)`).

**Decidido:**
- Cueva orgánica junto a la sala del jefe del piso 2, pasillo de 14 tiles, fosa sólida con E (igual que la del piso 1).
- Temas por celda: **Abismo → Someras (Shallows, intermedio) → Pantano (Swamp)** junto a la charca. Sin arte de tiles nuevo.
- **Charca del pantano** en lugar de la fosa azul: HECHA y aprobada en `docs/multimedia-temp/floor3-transition/`
  (`Transition_Pit_Swamp` 8 cuadros 112×80, `Transition_Bubble_Swamp` 4 cuadros que revientan; README con lo que
  necesita el código: fosa con arte por piso, burbuja con cuadros de reventón).
- Decoración del piso 3 junto a la charca (helechos, musgo; odres sin metano que explote). Mismo icono del mapa.
- Teletransporte en la antesala: la usuaria no lo marcó (no añadirlo sin preguntar).
- **Ambiente `Amb_Floor3_Loop`** HECHO (efectos_piso3.py + PROMPT_piso3.md; re y la, croar, insectos, lodo, gotas),
  pendiente de que la usuaria lo escuche en muestrario.html. Es también el ambiente del pantano verde.
- Bocas que ofrece: rana, Diplocaulus y vinagrillo. Metamorfosis por pareja con la subida (`floor3-metamorphosis`).

**CÓDIGO ESCRITO y compila (Sonnet, 2026-11-03), sin probar en Unity:** menú Primordia > Floor transition > Crear sala de paso del piso 2 (charca) y escena de prueba (FloorTransitionSetup.Floor2.cs: prefab TransitionPit_Swamp, ConfigureTransition en Floor_2 con Shallows y Swamp, Bocas del piso 3, enlace Floor_2→Floor_3, escena Test_Floor2Transition); FloorBuilder.ThemeTransition entra con la última franja (Abismo) y los helechos del pantano solo crecen en celdas Swamp fuera de la fosa; TransitionPit.SetBubblePop (burbuja que revienta); MusicDirector.NextAmbience (fundido hacia Amb_Floor3_Loop en el piso 2; hay que ejecutar Primordia > Audio > Importar audio); Floor2Setup ya no borra la sala de paso al repetirse. Falta que la usuaria la pruebe. Después: la metamorfosis por pareja.

(Antes del código, el plan era:) programar la sala (FloorTransitionSetup/Floor2Setup con temas Shallows/Swamp, fosa con arte
del piso, burbuja que revienta, ambiente nuevo en el fundido) y después la metamorfosis por pareja y el fondo de subida.

**Metamorfosis por pareja (Sonnet, 2026-11-03): CÓDIGO ESCRITO y compila, sin probar.** MutationData.MorphPath/MorphFrom(previous) (una hoja por Boca de origen; sin ella, la de la célula), TransitionPit pasa la Boca previa a Metamorphosis.Play(player, mouth, previous), Metamorphosis con modo `rising` (fondo Surface de 2 pantallas que baja durante la escena, rayos colgando de la superficie, motas que bajan despacio), ArtSetup.Floor3Metamorphosis.cs (ApplyFloor3Metamorphosis pone 9+6 hojas y Reveal en las 3 Bocas del piso 3; ImportRisingMetamorphosisArt). Todo lo ejecuta el menú Crear sala de paso del piso 2. Resúmenes de Bocas para las cartas: MutationData.Summary + MouthSummaries (aprobado). Falta que la usuaria pruebe la metamorfosis; la sala de cambio del piso 3 usará MorphFrom con las 6 hojas ya guardadas.
