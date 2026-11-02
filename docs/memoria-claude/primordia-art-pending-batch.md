---
name: primordia-art-pending-batch
description: HECHO Y MERGEADO (8ea22a8, 2026-11-02). Lista del arte que faltaba (iconos de mapa, corazón de sala, marca de regreso…) para generar de golpe con Opus cuando acaben los cambios de código de room-hearts y teleports
metadata:
  type: project
---

Acuerdo del usuario (2026-11-02): ir anotando el arte que falta y, **al terminar de corregir lo del código, cambiar a Opus y generarlo todo de golpe** (skill primordia-sprites; PREGUNTAR antes de dibujar, ver [[primordia-sprites-skill]] y [[primordia-token-saving]]). Avisar del cambio de modelo.

**DIBUJADO Y APROBADO (2026-11-02, Opus):** todo lo de abajo (1-4) en `docs/multimedia-temp/ui-extras/` (`generar_ui_extras.py` + README, entrada en MULTIMEDIA.md), SIN commit ni importar. Queda (Sonnet): rama desde main, menú `Primordia > Art > Importar extras de interfaz` que importe las hojas y sustituya lo provisional (RoomHeartPickup, HeartsDisplay.GreenFullSprite, MapTeleportIcons, PurpleHeartFx con aura/mota/onda) y el CÓDIGO del marcador de mapa del corazón sin recoger. Regreso es 9×9. El punto 5 (letrero con la fuente del juego) es código.

**Lista original:**
1. **Medio corazón rojo del suelo** (`RoomHeartPickup`, `Scripts/Rooms/RoomHearts.cs`, 8×11 px dibujado por código): idle con vaivén, recogida, y su vuelo a la vaina de almacén del piso 3 (la vaina `HeartPod_*` ya existe en floor3-environment/pieces).
2. **Icono de mapa «sala con medio corazón sin recoger»** (pedido del usuario: que sea imposible perdérselo). Falta también el CÓDIGO del marcador en `MapUI` (la sala con `RoomHearts.Dropped != null` y sin recoger; ver [[primordia-room-heart-map-icon]]); esa rama es `feature/room-hearts` y el mapa está en `feature/teleports`: coordinar al unirlas.
3. **Iconos de mapa de los teletransportes** (`UI/MapTeleportIcons.cs`: anillo aguamarina 7×7) y **marca de «regreso»** (círculo ámbar 7×7); quizá con variante por piso y estado atenuado/hover.
4. **Aura del corazón morado** (hoy provisional por código en `Core/PurpleHeartFx.cs`) y su onda: aviso del usuario (2026-11-02): **ligera, visible a simple vista pero que no cargue la vista ni tape la pantalla**. Corazón verde entero del HUD (hoy el lleno teñido por código en `HeartsDisplay`).
5. Posiblemente: letrero «Clic en un teletransporte…» con la fuente del juego (hoy es un `Text` de la fuente integrada, provisional).

**Why:** el usuario prefiere agrupar el arte en una sola tanda con Opus. **How to apply:** añadir aquí cada arte nuevo que falte; al abrir la tanda, leer esta lista y preguntar estilo/animaciones antes de dibujar.
