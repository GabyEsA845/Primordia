---
name: primordia-floors-design
description: "Rediseño del 2026-10-13: 6 pisos, criatura del jugador por piso × familia de arma, 4 mutaciones nuevas por piso, v1.0 con pisos 1-3"
metadata:
  node_type: memory
  type: project
  originSessionId: 5bb05669-19a4-44cc-90b6-804910fd3b05
  modified: 2026-10-14T02:52:44.000Z
---

Decidido por el usuario el 2026-10-13 (escrito en docs/GDD Primordia.md y docs/PLAN.md, sin commit hasta que lo revise):

- **6 pisos:** sopa primordial, océano cámbrico, pantano carbonífero, desierto pérmico, selva de dinosaurios, Edad de hielo. Cada uno con enemigos, jefe y 4 mutaciones nuevas (una por ranura) que se suman a las anteriores.
- **Forma del jugador:** sprites "medio sencillos". Una criatura por piso en 3 variantes según la familia de arma de la Boca (cuerpo a cuerpo / proyectil / área; sin Boca = proyectil), elegida al pasar de piso. El piso 1 sigue siendo la célula con piezas. Las demás mutaciones, capas sencillas encima; desde el piso 2 la Piel es una capa, no sustituye el cuerpo. 15 cuerpos nuevos en total.
- **v1.0:** pisos 1 a 3 (mínimo 2). Nuevas ramas previstas en PLAN: `feature/floor-transition`, `feature/floor-2`, `feature/floor-3`.
- Los nombres de criaturas, enemigos, jefes y las 20 mutaciones nuevas del GDD son borradores míos pendientes de que el usuario los revise.

**Why:** el sistema combinado ([[primordia-evolving-body]]) multiplicaba el arte; el usuario prefiere alargar el juego en pisos.
**How to apply:** no proponer de nuevo arte combinatorio; mantener el arte acotado a pisos × familias. Ver [[primordia-mutation-levels]], [[primordia-workflow]].

**Linajes (rediseño 2026-10-13, en el GDD sin commit):** sustituyen a Depredador/Acorazado/Tóxico (enum `Lineage` del código, que hoy colorea tarjetas, burbujas y tienda). Linaje = animal que se activa con 4 de 4 mutaciones (una por ranura); una mutación puede estar en varios. Clases: Carnívoro, Herbívoro, Venenoso, Hongo (beneficios por decidir). Piso 1 pasa a 12 mutaciones (+Hifas, Camuflaje, Vacuola digestiva) y 7 linajes: Tiburón, Calamar (carn), Tortuga (herb), Medusa, Escorpión (ven), Moho, Cuesco de lobo (hongo). Documento interactivo: https://claude.ai/artifact/1XpHC3PBSzqw1FGXMbScsL (beneficios, simulador, matriz, tabla de mutaciones con niveles I-III y bestiario de jefes con sprites; desde 2026-10-23 la fuente está en docs/multimedia-temp/documento-linajes/: template.html + build.py, que incrusta sprites e iconos y genera linajes.html para publicar con su url). Jefes: 2 por piso al azar; piso 1 = virus o ameba gigante (por hacer).

**Código de linajes (en main, merge e72c16c, 2026-10-14):** `LineageData` (SO, clase + 4 mutaciones por ranura, Apply/Remove vacíos), `PlayerLineage` (detecta el linaje completo con `MutationData.Lineages`, sin catálogo), `LineageLabel` en el HUD, menú **Primordia > Mutations > Crear linajes** (`Editor/LineageSetup.cs`, `SlotColors`). Se quitó el enum `Lineage`; burbujas, iconos (`Icon_Slot_*`) y colores van por ranura (Boca naranja, Extremidades morado, Piel azul, Órgano lima). Siguiente rama prevista: `feature/floor1-mutations` (Hifas, Camuflaje, Vacuola digestiva).

**Mutaciones nuevas del piso 1 (en main, rama `feature/floor1-mutations` fusionada):** Hifas (rastro de micelio; III red micelial), Camuflaje (los enemigos te buscan sin verte y te descubren si te acercas 1,5 s; III Pánico: matar desde el camuflaje asusta a la sala 2 s; NO reactiva el camuflaje, el usuario lo pidió así), Vacuola digestiva (se recarga matando: 4/3/3 muertes; cura 1/1/2 medios corazones). Menús: Primordia > Mutations > Crear mutaciones nuevas del piso 1 y Primordia > Art > Importar mutaciones nuevas del piso 1 (arte en docs/multimedia-temp/floor1-mutations/generar_mutaciones_piso1.py). `Weapon.SetDamageBonus` por fuente, `Enemy.AnyDied` estático, `IConcealable` en Core, estados Search/Scared y `Slow` en EnemyAI.

**Actualización 2026-10-23:** las "4 mutaciones nuevas por piso que se suman" y la elección del cuerpo al pasar de piso quedan sustituidas por [[primordia-floor-progression]] (mutaciones y linajes propios de cada piso, Boca obligatoria en la sala de paso, sala de cambio).
