---
name: primordia-floor2-rooms
description: "Diseño de las salas del piso 2 (cerrado 2026-10-24, en el GDD): fosas, 10 salas especiales con planos y arte, presentaciones de jefes; dónde está cada cosa"
metadata:
  node_type: memory
  type: project
  originSessionId: 71100f66-630c-4a00-8b6b-9be867d48a9e
  modified: 2026-10-25T04:25:40.881Z
---

Diseño de las salas del piso 2 **cerrado el 2026-10-24** con las ideas del usuario y **pasado al GDD/PLAN/MULTIMEDIA** (merge 3106a79). El detalle completo está en el GDD ("Piso 2: océano cámbrico", "Presentación de los jefes" y el registro "Decidido el 24 de octubre (salas del piso 2)"); aquí solo lo que no se deduce de él.

- **Números:** 10–12 salas de combate (las especiales con combate cuentan), recompensas tras 2/4/6/8/10, presupuesto 4 + 1,4, franjas por tercios.
- **Fosas** (idea del usuario, de Enter the Gungeon): andando caes (−½ corazón, vuelta al último suelo firme), esquivando cruzas (huecos ≤ 2 tiles), nadadores pasan con sombra separada, enemigo empujado muere, jefes no caen. Caída por código, sin arte por cuerpo.
- **10 salas especiales** (2–4 por partida; Parkour y Corazón morado son idea del usuario, las otras 8 mías y aprobadas). El **corazón morado** sale siempre en el Abismo del piso 2, para un golpe, persiste entre pisos y **abrirá una sala secreta del piso 3** (el usuario la contará más adelante).
- **Planos aprobados**, con contorno de cueva fijo por semilla (el usuario pidió que también fueran irregulares): docs/multimedia-temp/floor2-rooms/ (generar_salas_especiales.py → Layouts/Room_Special_*.txt en formato RoomTemplate con símbolos nuevos `~ o O F S C H E`, validación y catalogo.png). **Arte aprobado:** fosas por franja en environment/<Tema>/pits (16 tiles por máscara NESW + esquinas + motas), piezas en floor2-rooms/Pieces (almejas = cofres, coral del corazón, UI_Heart_Purple, WaveVent, sombras de nado, Fossil_Skeleton). README en floor2-rooms.
- **Presentación de jefes**: en main (merge f948d02, rama feature/boss-intro, probada por el usuario: "funcionan a la perfección"). Arte en docs/multimedia-temp/boss-intros/ (retratos 176 px, poses, nombres en Jersey 10 con color por piso). Anomalocaris y Euriptérido tienen ya su arte de presentación; falta conectarlo cuando existan esos jefes (añadirlos a `BossIntros` en ArtSetup.BossIntro.cs).
- **Documentos interactivos** actualizados y aprobados ("quedaron perfectos"): piso 1 https://claude.ai/artifact/1XpHC3PBSzqw1FGXMbScsL (v5: enemigos, siluetas/evolución de linajes, presentaciones) y piso 2 https://claude.ai/artifact/1Qdqi5ymyBEhETWY8MKmnm (v2: presentaciones y sección Salas). Fuentes en docs/multimedia-temp/documento-linajes y documento-piso2 (template.html + build.py).

El trabajo de código sigue en [[primordia-floor2-structure]]. Ver también [[primordia-floor2-design]] y [[primordia-floor-progression]].
