---
name: primordia-multimedia-temp
description: "Los recursos nuevos (sprites/animaciones/audio) y sus generadores van en docs/multimedia-temp/<recurso>/; desde el 2026-10-26 se sube a git (PNG/WAV/OGG por LFS) salvo GIF y cachés de Python; se importan a Assets con menús Primordia"
metadata:
  node_type: memory
  type: feedback
  originSessionId: ae24209d-d04a-463f-a03c-2043797eadd6
  modified: 2026-10-26T12:12:56.794Z
---

Desde el 2026-10-11 los recursos nuevos (sprites, animaciones, audio) y sus scripts generadores van en `docs/multimedia-temp/<recurso>/` (p. ej. `docs/multimedia-temp/floor2-mutations/`), y se importan a `Assets/_Primordia/...` con los menús `Primordia > …` del editor.

**Cambio del 2026-10-26:** el usuario iba a trabajar desde otro PC y decidió **subir la carpeta a git** (en la rama feature/floor2-mutations, commit de ese día): `.gitignore` ya no la ignora entera, solo `/docs/multimedia-temp/**/*.gif`, `__pycache__/` y `*.pyc`. Los PNG, WAV, OGG y TTF van por Git LFS (.gitattributes). Antes estaba ignorada para no duplicar originales en el repo; ahora prima poder clonar y que los menús y generadores funcionen.

**How to apply:** el arte nuevo sigue generándose en `docs/multimedia-temp/<recurso>/` (con su README); se versiona junto con el código. No añadir GIF de revisión al repo. Documentar las importaciones en docs/MULTIMEDIA.md. Ver [[primordia-sprites-next]] y [[primordia-workflow]].
