---
name: primordia-exchange-room
description: "feature/exchange-room TERMINADA y en main (merge 4ff7147, 2026-10-25): sala de cambio / laboratorio de Darwin con cápsula, ventana, reglas y metamorfosis en la cápsula; documento del piso 2 v6"
metadata:
  node_type: memory
  type: project
  originSessionId: 011f772f-7b22-42cb-9e0a-191cdf7dc6d7
  modified: 2026-10-26T01:16:44.186Z
---

Rama **`feature/exchange-room`** (sale de main 95e1189, 2026-10-25). Pasos en PLAN.md: 1 cápsula + reglas + ventana + F7 · 2 metamorfosis en la cápsula + documento del piso 2.

**Decisiones del usuario (2026-10-25):** las mutaciones de un piso anterior se pueden entregar y la nueva entra **un nivel por debajo** (mínimo I); las **especiales** de las almejas ni se dan ni se reciben; si cambia la familia de la Boca, **metamorfosis corta dentro de la cápsula** (no la de la fosa). Interfaz aprobada: criatura con ranuras a la izquierda (clon de la pausa), 2 tarjetas a la derecha, Aceptar/Salir, gastada tras usarla.

**Arte aprobado:** docs/multimedia-temp/exchange-room/generar_sala_cambio.py + README: Capsule_Idle/Glow/Flash/Used (40×48, apoyo abajo en el centro), Morph_<A>_<B> (6 parejas, 32×32); la revelación reutiliza ../metamorphosis/Reveal_<B>.

**Paso 1 (commit bd83e39):** Mutations/MutationExchange (CanGive, LevelFor, Options filtra pool por ranura, no especial, no la actual, Floor == piso; Apply = Equip + LevelUp; ChangesBody). Rooms/ExchangeCapsule : PromptInteractable (idle/used, SetFloor(pool, piso), Exchange con HoldForm/ReleaseForm, evento Exchanged). UI/ExchangeUI (panel, CreaturePanel clonado, botones y marcas por ranura, 2 MutationCard con ShowExchange nuevo, Aceptar/Salir, Esc, estado MutationChoice). FloorConfig.exchangeCapsule + FloorBuilder la pone en la K (BlockCorners) de la sala Exchange con luz verde; Mark.Capsule ahora es obstáculo. Editor/ExchangeRoomSetup, menú **Primordia > Rooms > Crear sala de cambio** (importa arte a Art/Environment/Exchange, prefab Prefabs/Rooms/ExchangeCapsule, Floor_2, ventana en GameUI clonando PausePanel/Creature, una tarjeta de MutationChoicePanel, un botón de la pausa y TitleBanner). DebugCheats **F7** lleva junto a la cápsula. ExchangeTests (6). Ajuste pedido por el usuario: las tarjetas del cambio usan CurrentDescription (base + solo la mejora de su nivel) y la descripción encoge la letra (best fit, mínimo 6, LayoutElement preferredHeight 1 + flexible) para no salirse.

Ver [[primordia-floor-progression]], [[primordia-floor2-rooms]], [[primordia-workflow]].

**Paso 2 (commit eafb133; merge 4ff7147 en main, push de main y rama; GDD con las reglas; documento del piso 2 v6 con la sección de la sala de cambio):** ExchangeCapsule.Exchange lanza Metamorphose si cambia el cuerpo (estado Cinematic, sin física, el jugador se desvanece hacia el vidrio, Glow teñido del color de la familia, Silhouette en el centro del vidrio con Morph por pareja de familias y RevealFrames de la Boca nueva, ReleaseForm, sale en exitOffset (0,-1,7) con PlayAttack(Down) y el destello) o Shine (brillo 0,6 s). Morph[] (from, to, frames) lo rellena el menú desde Morph_<Cuerpo>_<Cuerpo> (Jellyfish=Proyectil, Placoderm=Cuerpo a cuerpo, Crinoid=Área). 2 tests más (8).
