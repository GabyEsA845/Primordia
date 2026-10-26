---
name: primordia-floor2-structure
description: "Estructura del piso 2 (feature/floor-2-structure, en main 59f633c desde 2026-10-25): qué hay, dónde está y qué es provisional"
metadata:
  node_type: memory
  type: project
  originSessionId: 576d87a4-478e-4101-8073-55f78fe1107a
  modified: 2026-10-25T16:19:05.929Z
---

Rama **`feature/floor-2-structure`** (sale de main f948d02): pasos 1–4 hechos y probados; **merge a main 59f633c (2026-10-25)** con push y casilla marcada. Lo siguiente, en [[primordia-clam-mutations]].

Commits: 7a84abc (1a paso al piso 2 en la misma escena y franjas) · 8a3c98d (1b cuevas, túneles, camino que baja) · 037c4d4 (1c losas, cobertura, cáusticas, rayos, nieve, luces) · 65d2f46 (paso 2 especiales, Parkour, sala de cambio, antesala) · 56aae9a (atajos F1–F4 y TestLoadout) · c46d656 (paso 3 fosas) · 8544d40 (paso 4 almejas, corazón morado, oleadas, ajustes de fosas).

Lo que no se deduce fácil del código:
- **Menú** Primordia > Rooms > Crear piso 2 y escena de prueba (Floor2Setup.cs) monta todo: temas Shallows/Cambrian/Abyss, Floor_2, catálogo de cuevas, planos de docs/multimedia-temp/floor2-rooms/Layouts → Data/Rooms/Floor2 (no sobrescribe), piezas, nieve, HDR del Renderer2D a 2 (luces en WebGL), Test_Floor2 con TestLoadout (Boca del piso 2 + una mutación por ranura a nivel II). Tras cambiar arte o planos hay que volver a lanzarlo.
- **Atajos de prueba** (DebugCheats, solo editor/dev build): F1 invulnerable, F2 daño ×5, F3 vacía la sala, F4 mutación al azar, F5 especial de las almejas, F6 siguiente Boca del piso con el mismo nivel (pedido el 2026-10-25 para probar las 3 Bocas en Test_Floor2; en el editor busca las Bocas con AssetDatabase). El usuario los usa porque con el daño inicial el piso 2 es duro.
- **Generador**: el piso 1 debe dar el mismo plano con la misma semilla (no gastar azar en lo que el piso 1 no tiene). FloorLayout usa huecos (Slot) con direcciones obligadas (Parkour bajando, antesala en línea con el jefe); Link guarda la dirección (layout.Direction(i)). Salas de 2 casillas en FloorBuilder.Anchors.
- **Planos**: generar_salas_especiales.py y generar_salas_fijas.py (comprobación + vistas previas con tilesets reales). Símbolos: # . ^ v < > o O ~ F S C H E A L K R.
- **Fosas**: caída medida en los pies (footOffset −0,3), margen 0,25 y 0,08 s; enemigos que andan no pisan fosa salvo empujados (EnemyAI.AvoidPits); nadadores con EnemyData.swims (ninguno aún).
- **Provisional**: enemigos, jefes (en las salas del Anomalocaris y Euriptérido) y mutaciones del piso 1; música del piso 1; el piso 2 no tiene sala de paso (su salida da la victoria). Sin efecto todavía: cápsula K (feature/exchange-room), algas A y plantas L (jefes), esponja S (enemigos), rocas R que se derrumban (Euriptérido), puerta de losas grande de la antesala (sin arte). Almejas grandes sin mutación hasta diseñar las especiales.

Siguientes ramas del PLAN para el piso 2: exchange-room, floor2-mutations, floor2-lineages, floor2-enemies, boss-anomalocaris, boss-eurypterid, floor2-audio. Ver [[primordia-floor2-rooms]], [[primordia-floor2-design]], [[primordia-workflow]].
