---
name: primordia-floor2-transition-room
description: "Sala de paso del piso 2 al 3 (charca del pantano), ambiente del pantano y metamorfosis por pareja: HECHAS, probadas y subidas (2026-11-03)"
metadata:
  type: project
---

**HECHO y probado por la usuaria (commit d9244789).** La salida del piso 2 ya no da la victoria: se salta a la charca.
- Menú `Primordia > Floor transition > Crear sala de paso del piso 2 (charca) y escena de prueba` (`FloorTransitionSetup.Floor2.cs`):
  prefab `TransitionPit_Swamp`, `Floor_2.ConfigureTransition(cave, pit, Shallows, Swamp)`, Bocas del piso 3 (rana, Diplocaulus,
  vinagrillo), enlace `Floor_2 → Floor_3`, escena `Test_Floor2Transition` (F3 vacía la sala del «jefe»). Repetirlo es seguro;
  `Floor2Setup` ya no borra la sala de paso.
- Cueva Abismo (última franja) → Someras → Pantano; helechos del pantano solo en celdas Swamp lejos de la fosa; burbuja que revienta
  (`TransitionPit.SetBubblePop`); fundido de ambiente hacia `Amb_Floor3_Loop` (`MusicDirector.NextAmbience`: piso 1 → océano, piso 2 → pantano).
- Metamorfosis por pareja: `MutationData.MorphPath/MorphFrom(previous)`, `Metamorphosis.Play(player, mouth, previous)`, modo `rising`
  (fondo `Surface_*` que baja, la criatura sube); `ArtSetup.ApplyFloor3Metamorphosis` guarda 9 + 6 hojas y los Reveal.
  La sala de cambio del piso 3 (paso 4) puede usar `MorphFrom` con las 6 hojas entre cuerpos del piso 3 (la cápsula del piso 2 tiene sus propias hojas).
- Ambiente `Amb_Floor3_Loop` (`efectos_piso3.py`, `PROMPT_piso3.md`; re y la, croar, insectos, lodo, gotas) para el pantano verde; los ambientes de
  bosque humeante y tierra quemada, la música y el resto del audio del piso 3 van en el paso 8.
- Resúmenes de una frase para las cartas de Boca: `MutationData.Summary`, menú `Primordia > Mutations > Resumir las descripciones de las Bocas`.
