---
name: primordia-mutation-levels
description: Diseño acordado (2026-10-13) de niveles y evoluciones de mutaciones en Primordia; pendiente de arte e implementación
metadata:
  node_type: memory
  type: project
  originSessionId: b64a33a5-4861-4dbc-9a77-eb8392639df9
  modified: 2026-10-13T14:32:17.726Z
---

Rama prevista `feature/mutation-levels`. Minimapa ya en main (merge 8edc78c).

Decisiones del usuario (2026-10-13):
- 3 niveles. Nivel II = mejora de valores; nivel III = evolución (efecto nuevo).
- Se sube eligiéndola otra vez en la elección 1 de 3 (y con biomasa cuando exista la tienda: dejar un método para subir de nivel que la tienda use luego).
- Al reemplazar en la misma ranura, el nivel se pierde.
- Oferta: si hay alguna mutación equipada que pueda subir, al menos 1 de las 3 cartas es mejora.
- Aspecto: insignia de nivel (puntos/gemas) sobre el icono en HUD, tarjetas y pausa. Tarjetas de mejora muestran "Nivel I → II".

Tabla aprobada (con cambios del usuario):
- Mandíbulas II: daño 3→4, alcance +15 %. III Desgarro: sangrado 1/s 3 s + cura cada 2 muertes. Visual: marca de sangrado en enemigos y efecto de curación en el jugador.
- Aguijón II: veneno 1→2/s. III Contagio: envenenado que muere estalla (radio 2). Sprite del aguijón rehecho como cola de avispa (colores Tóxicos).
- Esporas II: 3→5, abanico 30°→45°. III: esporas buscadoras.
- Aletas II: +45 % velocidad, recarga esquiva −25 %. III Emboscada: +50 % daño 1,5 s tras esquivar, con aura visible.
- Patas II: aturdimiento 1 s, daño 2. III Pisotón: onda radio 2 al acabar la esquiva que aturde (con animación). Marca de aturdido.
- Caparazón II: +2 corazones total, −5 % velocidad. III Placas: anula el primer golpe por sala. Sprite rehecho como caparazón de tortuga en la espalda.
- Piel urticante II: 3 de daño y envenena. III Erizo: 8 púas al recibir golpe.
- Tinta II: radio 4, recarga 6 s. III Tinta corrosiva: 1 daño/s dentro de la nube. Nube más transparente; ahora dura 3 s (Start/Loop/End). Marca de cegado.
- Glándula de choque II: radio 4→6, recarga 6→4,5 s. III Paralizante: paraliza 2 s a los golpeados (marca + rayos en el cuerpo). Se descartó el "reflejo" por roto.

Encargo de arte escrito en `docs/multimedia-temp/evolutions/PROMPT.md`; esperando el arte. Al implementar: pasar la tabla al GDD. Ver [[primordia-sprites-next]] y [[primordia-workflow]].

Implementado y fusionado en main (2026-10-13, commit cd16ce1, merge b751d48; el usuario pidió cerrar la sesión con commit). `MutationData.Apply/Remove(player, level)`, `PlayerMutations.LevelOf/LevelUp/CanLevelUp` + evento `LevelChanged` (Equip de una equipada = subir nivel; reemplazar pierde nivel). Datos por nivel en cada subclase (`upgrades[]`, nivel I en los campos de siempre). `MutationPool.PickOffer(count, PlayerMutations)` garantiza 1 mejora. Nuevos: `DamageOverTime` (Poison con contagio, Bleed), `StatusMarkers` (marcas sobre la cabeza), `EnemyAI.Paralyze`, `Health.Shields`, `Weapon.DamageMultiplier`, proyectiles buscadores, `Ambush`, `Plates`, `QuillBurst`, `InkCloudArea` (nube de 3 fases), `LevelUpEffect`, `LevelBadge`. Editor: **Primordia > Art > Importar evoluciones** (arte + llama a **Primordia > Mutations > Configurar niveles y evoluciones**, `MutationLevelsSetup`). Los menús antiguos ya no pisan el arte rehecho de Aguijón/Caparazón ni la tinta nueva. GDD con la tabla de niveles. Truco de prueba: poner la misma mutación 3 veces en Starting Mutations del jugador la deja en nivel III.
