# Primordia

Roguelike de acción con vista cenital, hecho en Unity + C# para la GameJam 2026.
Una criatura primitiva cruza salas, derrota enemigos y muta su cuerpo para sobrevivir,
inspirado en *Enter the Gungeon* y *Everything is Crab*.

- **Documento de diseño:** https://claude.ai/code/artifact/ff1c1d95-cc54-460a-b902-5eb9b297c4bf
- **Motor:** Unity 6 (o 2022 LTS), plantilla 2D URP
- **Estilo:** pixel art, 16 px por unidad

## Cómo abrir el proyecto

1. Instala Git LFS (`git lfs install`) antes de clonar.
2. Clona el repositorio y ábrelo desde Unity Hub con la versión indicada en `ProjectSettings/ProjectVersion.txt`.

## Estructura de carpetas

```
Assets/_Primordia/
  Art/          sprites, tilesets, animaciones
  Audio/        música y efectos
  Data/         ScriptableObjects (mutaciones, enemigos)
  Prefabs/      jugador, enemigos, salas, proyectiles
  Scenes/       MainMenu, Game
  Scripts/      Player, Enemies, Mutations, Rooms, Core, UI
```

## Controles

WASD mover · Ratón apuntar · Clic izquierdo atacar · Espacio esquivar · Clic derecho habilidad · Esc pausa
