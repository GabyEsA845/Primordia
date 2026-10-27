---
name: primordia-boss-eurypterid
description: "Jefe Euriptérido terminado y en main (merge 38cd9f2, 2026-10-27): cueva que se derrumba, fases, último aliento con bullet hell, barra propia; cifras y decisiones del usuario"
metadata:
  type: project
---

**TERMINADO y en main** (merge 38cd9f2, 2026-10-27; push de main y de feature/boss-eurypterid). El usuario lo probó todo. Commits: e4e3584 sala y derrumbe, 16c3fdd fase acorazada, 36fda67 muda y fase blanda + arreglo del temblor, 863e1c8 último aliento, 878dc01 más dificultad, 527a8ec barra, 92942dc GDD/PLAN/documento.

## Lo que pidió el usuario por el camino (no estaba en el GDD original)
- **Derrumbe de la cueva** (CaveCollapse): crujidos (fase 1) → terremoto (muda, derrumba las rocas R) → derrumbe permanente (piedras del techo con sombra, 3–4 zonas Z que se hunden en fosa). Las piedras dañan y aturden al jefe (IFallingRockTarget).
- **Temblor de pantalla** (Core/ScreenShake, para todo el juego) y opción **Temblor** normal/suave/no en el panel AUDIO de la pausa. Arreglo clave: la PixelPerfectCamera de URP fija worldToCameraMatrix al empezar a dibujar la cámara, así que el temblor mueve la cámara en beginContextRendering.
- **Último aliento** (EurypteridBoss.LastStand.cs): a 0 no muere; escena al centro, cae, se levanta, bullet hell de ~25 s inmune con la barra que se vacía sola (Health.Drain) y muere solo. Pidió **más dificultad** (subida ~25 %, aprobada).
- La muda vacía **se rompe a 15 golpes** (EurypteridExuvia); gotas y púas la desgastan.
- Rocas R movidas a las columnas 8-9 y 22-23 del plano (estorbaban al jefe en el centro).
- Barra propia con un marco por fase (BossBarSkin.PhaseLook.frame) y efecto de cambio de fase (phaseChange), reutilizables.

## Piezas para reutilizar
PlayerController.Slow + Combat/Venom (ralentiza con tinte verde); Health.Drain; NavigationGrid.SetWalkable y RoomPainter.RepaintPits (fosas en tiempo de juego); marca Z en RoomTemplate; F9 (rocas) y F10 (fase de la cueva) en DebugCheats; menú Primordia > Boss > Crear Euriptérido y escena de prueba (relee el plano de su sala, que Floor2Setup no sobrescribe).

## Pendiente opcional
Aviso de pinzas propio en rosa (ahora congela el cuadro 0 de DoublePincer y destella): ofrecido, sin pedir.

**How to apply:** referencia para los jefes siguientes (mismo esquema y ciclo de [[primordia-workflow]]); sprites con [[primordia-sprites-skill]]. Siguiente módulo: feature/floor2-audio.
