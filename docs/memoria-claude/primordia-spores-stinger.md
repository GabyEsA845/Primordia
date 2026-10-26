---
name: primordia-spores-stinger
description: Rediseño aprobado 2026-10-15 del arte del Aguijón (más pequeño) y de las Esporas (gorrito de hongo + bocanada que flota); rama feature/mouth-tweaks antes de lineage-benefits
metadata:
  node_type: memory
  type: project
  originSessionId: 8d59c261-1e60-421a-9039-00c7c87f936d
  modified: 2026-10-15T13:42:06.108Z
---

Decidido por el usuario el 2026-10-15, antes de programar los beneficios de linajes ([[primordia-lineage-benefits]]):

- **Rama propia** `feature/mouth-tweaks` desde main; después se sigue con `feature/lineage-benefits` (ahí quedan las ediciones del GDD/PLAN de linajes, sin commit).
- **Aguijón:** pieza de Boca a ~2/3 (unos 16 px de largo y algo más fina), también la versión de la evolución (Contagio); el proyectil sale de la punta.
- **Esporas:** fuera la pieza de Boca de plano. Gorrito de hongo **Amanita** (rojo con lunares blancos) como capa sobre la cabeza de cualquier cuerpo/Piel, en 3 direcciones, siguiendo el balanceo. Ataque **por código**: el cuerpo entero con sus piezas se adelanta 2-3 px hacia el cursor y vuelve; el gorrito se aplasta y se hincha al soltar.
- **Mecánica bocanada:** I = 5 esporas en abanico de 75°, frenan en 0,6 s y flotan 1,5 s dispersándose a 1,5 u/s con vaivén (el usuario pidió más dispersión; daño ya mínimo, se bajó la cadencia a 1,25/s), 1 de daño. II = 7 esporas, flotan 2,5 s. III Esporas buscadoras = las que flotan derivan despacio hacia el enemigo más cercano (radio 3).
- Esporas color crema con borde rojizo (distintas de las rosas enemigas).
- **Arte lo genero yo** con script Python en docs/multimedia-temp y enseño vista previa ampliada antes de importar.

**Why:** al usuario no le gustaba la Boca de las Esporas ni el tamaño del Aguijón.
**How to apply:** actualizar GDD (tabla de valores y niveles de Esporas, "Aspecto de las mutaciones") al implementarlo; seguir [[primordia-workflow]].

**Mandíbulas (añadido 2026-10-15, misma rama):** desde el nivel II (también en III Desgarro) la mordida da un impulso corto de 0,6 u hacia el cursor (~0,1 s), sin invulnerabilidad, frenado por paredes y enemigos, para alcanzar a quien retrocede.

**Estado 2026-10-15:** confirmado por el usuario y en main (commit b5e02d7, merge 189834f). Arte: `docs/multimedia-temp/mouth-tweaks/generar_bocas.py` (copia Aguijón a evolutions/Mouth, espora a projectiles/, iconos a ui/Icons; originales en el scratchpad de la sesión). Menú **Primordia > Art > Importar ajustes de las Bocas** (`Editor/ArtSetup.MouthTweaks.cs`). Código: `AttackData` brake/jitter/drift/fade + meleeLunge, `Projectile.Float`, `Weapon` golpe diferido con `IMeleeLunger` (PlayerController.Lunge con Rigidbody2D.Cast), `MutationData` wornOnHead/muzzleDistance/attackLean/headHeight, `MutationLook` pieza `Cap` (orden 13) + adelanto y aplastado, `CreaturePortrait` con gorrito. Las ediciones de linajes siguen en `git stash` ("lineage-benefits: GDD y PLAN...").
