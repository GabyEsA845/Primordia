---
name: primordia-audio
description: Audio del piso 1 (música, 113 efectos, volumen en la pausa) en main desde el 2026-10-23, merge ef052ce
metadata:
  node_type: memory
  type: project
  originSessionId: 5fd902a4-0bd3-4d87-8a22-abb4df8d5c29
  modified: 2026-10-23T21:13:19.065Z
---

El 2026-10-23 se generó toda la música y los efectos del piso 1 según `docs/multimedia-temp/audio/PROMPT.md` (aprobado por el usuario): 157 `.wav` en `SFX/` (113 sonidos con variantes `_01`..`_03`) y 11 `.ogg` en `Music/`. Se sintetiza todo con `generar_audio.py` (+ `sintesis.py`, `efectos.py`, `musica.py`; numpy y scipy instalados en el Python 3.12), que comprueba los nombres contra las tablas del PROMPT y crea `muestrario.html` (volumen propuesto por sonido, «Empalme ×2», «Ritmo real»). `python generar_audio.py Nombre` regenera uno solo.

Las capas `Music_Floor1_Explore`/`_Combat` duran lo mismo (32 compases a 100 BPM) para fundirlas al cerrar/abrir puertas; `Music_Boss_Amoeba_Final` dura exactamente 30 s.

**Integración (en main, merge ef052ce, 2026-10-23):** `Scripts/Audio`: `AudioManager` se crea solo desde `Audio/Resources/SoundLibrary` (sin mezclador de Unity: WebGL no tiene filtros), `MusicPlayer` (dos platos, capa sincronizada, fragmentos, atenuado en pausa), `MusicDirector` (por escena, sala vía eventos estáticos de `Room` y `GameState`), `EffectSound` en prefabs de efectos, `PlayerAudio` y `ButtonSounds` añadidos en tiempo de ejecución, `Sounds` con los nombres. Campos de sonido en `AttackData`, `EnemyData`, `MutationData`. Menús **Primordia > Audio > Importar audio** y **Añadir volumen a la pausa** (`AudioVolumes` en PlayerPrefs, `VolumeSlider`, sprites de `docs/multimedia-temp/menus/generar_volumen.py`).

**Why:** módulo `feature/audio` del PLAN, aprobado por el usuario ("funciona bastante bien").
**How to apply:** un sonido nuevo = añadirlo a efectos.py, regenerar e Importar audio; si depende de datos, rellenar el campo del asset en AudioSetup. Probado en una build WebGL subida a itch.io el 2026-10-23: funciona. Ver [[primordia-multimedia-temp]], [[primordia-workflow]].
