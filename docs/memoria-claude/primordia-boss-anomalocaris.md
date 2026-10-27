---
name: primordia-boss-anomalocaris
description: "Jefe Anomalocaris terminado y en main (merge 1086049, 2026-10-26): sala, fases, barra de vida propia y barra fantasma, Tab de tres estados"
metadata:
  node_type: memory
  type: project
  originSessionId: c8d2afc4-ef5b-4f47-b591-8e5a73da4f56
  modified: 2026-10-26T23:17:31.606Z
---

**TERMINADO y en main** (merge 1086049, 2026-10-26; último commit de la rama 438f2f0; push de main y de la rama). El usuario lo probó todo (barra y Tab incluidos).

Último paso (438f2f0): barra fantasma para todos los jefes (BossHealthBar); aspecto propio por jefe con BossBarSkin (ScriptableObject en Data/Bosses) + BossBarAppearance (en el prefab del jefe) y fases con IPhasedBoss (IBoss.cs). El Anomalocaris tiene una barra de 258×32 con la ventana en (38, 12), muescas al 60 % y al 25 % y silueta cuando es inmune. La cabeza se rediseñó a petición del usuario, porque la redonda "no daba miedo": ahora tiene el escudo en punta, fauces de garras con colmillos y ojos con ceño. Generador: floor2-bosses/generar_barra_anomalocaris.py. Lo importa ArtSetup.CreateAnomalocarisBarSkin desde el menú del jefe. Se puede reutilizar para el Euriptérido (darle un BossBarSkin propio si se quiere).

`ProjectSettings.asset` sigue modificado por Unity desde antes (preloadedAssets vacío): no incluirlo en commits salvo que el usuario lo pida.

## Qué hay hecho (en la rama, probado y aprobado)

- **Sala** (59fd11b): arte de algas, plantas luminosas y burbuja (`floor2-bosses/generar_sala_anomalocaris.py`); AlgaeColumn (orden 25, translúcida con el jugador debajo), LightPlant (SetLit, SetDimmed), RoomDarkness (en salas con marcas L: al entrar luz global 0,12 azulada + halo 2,5 u en el jugador; al despejar vuelve). Ni algas ni plantas a menos de 3,5 u de donde aparece el jefe. Menú **Primordia > Boss > Crear Anomalocaris y escena de prueba** → Test_BossAnomalocaris.
- **Jefe** (7785680, a1e3ccb): AnomalocarisBoss (+ .Phases.cs, partial), AnomalocarisBubble, AnomalocarisMarkers; 300 de vida, biomasa 10, sin mutación. Acecho: óvalo junto a las paredes con estela de burbujas; cada 3–4,5 s aviso 0,7 s apuntando adonde irá el jugador y dash a 22 u/s que muerde (1 corazón) y 1 s vulnerable. Cerco (60–25 %): espiral con muro de burbujas y luego el dash. Escena de los ojos (25 %, ~3,5 s, se salta con E; antes no baja del 25 %). Frenesí en ciclo: dash largo marcado (~1 s) → salto a morder → aturdido 0,5 s → persecución 2,5 s → segundo dash con marca corta → silueta invulnerable 3 s con plantas atenuadas → repite. Sonidos en floor2-audio.
- Pruebas: AnomalocarisRoomTests, AnomalocarisBossTests, AnomalocarisPhasesTests (448/449 en verde; la que fallaba era la esquiva a cámara lenta, ya arreglada).

**How to apply:** referencia para el Euriptérido (mismo esquema: arte de sala → jefe → fases → barra → cierre). Seguir [[primordia-workflow]] y hacer todo sprite nuevo con la skill [[primordia-sprites-skill]].
