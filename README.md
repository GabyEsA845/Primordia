# Primordia

Roguelike de acción con vista cenital, hecho en Unity + C#.
Una criatura primitiva cruza salas, derrota enemigos y muta su cuerpo para sobrevivir.

- **Plan de desarrollo:** [docs/PLAN.md](docs/PLAN.md)
- **Documento de diseño:** [docs/GDD Primordia.md](docs/GDD%20Primordia.md) (versión anterior publicada: https://claude.ai/code/artifact/ff1c1d95-cc54-460a-b902-5eb9b297c4bf)
- **Motor:** Unity 6 (6000.6.2f1), plantilla 2D URP
- **Estilo:** pixel art, 16 px por unidad

## Cómo abrir el proyecto

1. Instala Git LFS (`git lfs install`) antes de clonar.
2. Clona el repositorio y ábrelo desde Unity Hub con la versión indicada en `ProjectSettings/ProjectVersion.txt`.

## Estructura de carpetas

```
Assets/_Primordia/
  Art/          sprites, tilesets, animaciones
  Audio/        música y efectos
  Data/         ScriptableObjects (ataques, mutaciones, enemigos)
  Prefabs/      jugador, enemigos, salas, proyectiles
  Scenes/       MainMenu, Game
  Scripts/      Core, Player, Combat, Enemies, Mutations, Rooms, UI
```

## Controles

WASD mover · Ratón apuntar · Clic izquierdo atacar · Espacio esquivar · Clic derecho habilidad · Esc o P pausa

## Publicar en itch.io (build web)

1. En Unity: **Primordia → Build → Build WebGL (itch.io)**. La build queda en `Builds/WebGL` (Git la ignora).
2. Comprime en un `.zip` el **contenido** de `Builds/WebGL`: `index.html` debe quedar en la raíz del zip, no dentro de una carpeta.
3. En itch.io, en la página del juego: *Kind of project* → **HTML**, sube el zip y marca **This file will be played in the browser**.
4. En *Embed options*: tamaño **960 × 540** y activa **Fullscreen button**.
