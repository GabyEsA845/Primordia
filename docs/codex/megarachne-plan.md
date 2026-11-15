# Megarachne — plan de subpasos (escrito 2026-11-15 por Claude; Codex implementa tareas pequeñas)

Minijefe opcional del piso 3, sala lateral «el Nido» (Bosque humeante 32×20 u, cámara ×1). Arte aprobado y subido: `docs/multimedia-temp/floor3-bosses/megarachne/README.md` (fuente de verdad de hojas, anclas y pivotes). Combate y decisiones de arte: `docs/memoria-claude/primordia-boss-megarachne.md`. Rama `feature/boss-megarachne` (desde `feature/boss-arthropleura`); ramas de Codex `codex/mega-*` desde ella. Menú `Primordia > Boss > ...`, escena `Test_BossMegarachne` con teclas de prueba, pruebas PlayMode. Modelo de referencia de código: `LepidodendronBoss.*.cs` y `ArthropleuraBoss.*.cs` (partial classes por tema).

## Números CERRADOS (2026-11-15)
- Vida **150**, enfado al **50 %** (muesca). Combate objetivo 60-90 s.
- Daño al jugador: **salto 2, red 1 (atrapa 1 s), crías 1, bolita de seda 1**.
- Solo recibe daño **en el suelo** (en los cables, invulnerable). Ventanas: tras el salto ~1,5 s; cable quemado con ella encima = cae aturdida ~3 s; al tejer cable nuevo cruza por el suelo (hilo fino vulnerable).
- **Recompensa:** corazón (cura) + algo de biomasa + **interfaz de tarjetas con las mutaciones equipadas que no sean de nivel 3; elegir una la sube un nivel** (como la interfaz de recompensa de mutaciones: `MutationChoiceUI`, `MutationCard`, `MutationReward`). Si no hay ninguna subible, solo corazón + biomasa. Sustituye al «contenedor de corazón» del diseño; el capullo de seda del arte sirve como contenedor (cae del embudo).

## Subpasos
| | Qué | Quién |
|---|---|---|
| M1 | Importar arte (menú), sala Nido 32×20, árbol empapado con copa translúcida, 5 cables a Sigillarias/tocones, `MegarachneBoss` con vida, `Test_BossMegarachne`, movimiento por cables/suelo (16 direcciones) | Codex (tarea 13) |
| M2 | Ciclo de ataques: salto desde el cable (marca, cae, vuelve trepando), ráfaga de seda (5-7 bolitas + telarañas que frenan y arden), red que cae, ventanas de daño | Codex |
| M3 | Saco de crías (revienta en 3-4 crías; romperlo antes lo evita) y crías propias (Move/Bite/Hurt/Death) | Codex |
| M4 | Fuego en cables: mecha ~6 u/s en ambos sentidos, cable carbonizado, cae el cable y ella si la alcanza (aturdida 3 s), ascuas que prenden telarañas/helechos, fuentes de fuego que rebrotan | Codex |
| M5 | Enfado al 50 %: vuelve al Nido, chilla (árbol se seca, lluvia), sale en Rage, Nido arde una vez (gran ventana), cables nuevos desde el tronco más rápido | Claude/Codex |
| M6 | Entrada (embudo, ojos, baja colgada, BossIntro con retrato «La araña que nunca fue» + sello MINIJEFE), barra con piel `BossBar_Megarachne`, muerte (panza arriba, red se descuelga), recompensa | Claude |
| 8 | Sonido, enganche a Floor_3 (sala lateral), documento del piso 3 | después |

Pendiente de cerrar al llegar a cada subpaso: tiempos del ciclo, velocidades, número de cables nuevos en el enfado.
