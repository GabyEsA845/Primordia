# Tarea 15 — Megarachne: ciclo de ataques desde los cables (M2)

- **Rama:** `codex/mega-2-ataques` (desde `feature/boss-megarachne`, ya con M1)
- **Escrita por:** Claude (2026-11-15) · **Implementa:** Codex
- **Estado:** pendiente

## Objetivo
La Megarachne vive en los cables (invulnerable) y ataca desde ellos; solo baja al suelo (vulnerable) tras saltar sobre el jugador. Ya NO persigue por el suelo. Sin saco/crías (M3), fuego (M4), enfado (M5), entrada/barra/muerte (M6).

## Contexto mínimo
`AGENTS.md`, `docs/codex/megarachne-plan.md`, `docs/memoria-claude/primordia-boss-megarachne.md` (Ronda 2 y 3), README de `floor3-bosses/megarachne/` (hojas DropWarn, Land, Fall, Stunned, Recover, SilkShoot, Weave, origen de la seda por dirección; Effects/Proj_MegarachneSilk, Fx_MegarachneWeb_*, Fx_MegarachneNet_*, Fx_MegarachneDrop_Track/Locked, Fx_MegarachneDust). Código actual: `MegarachneBoss.cs`, `MegarachneBoss.Cables.cs`. Modelo de ataques: `LepidodendronBoss.Attacks.cs`, `ArthropleuraBoss.Phase2.cs`.

## Qué hacer
1. **Comportamiento base:** al empezar y tras cada ataque vuelve/queda en un cable. En cable corre entre anclas (`CableSpeed` 7 u/s) y se coloca en el cable más cercano al jugador (sin pararse más de ~1 s). Quitar la persecución por el suelo y la subida automática cada 6 s.
2. **Ciclo:** un ataque cada **4 s**, rotando salto → ráfaga → red → (repite). Archivo nuevo `MegarachneBoss.Attacks.cs`.
3. **Salto:** desde el cable, `DropWarn` 1 s con marca `Fx_MegarachneDrop_Track` siguiendo al jugador y `Fx_MegarachneDrop_Locked` fija los últimos 0,3 s; cae (`Drop`) a esa marca, `Land` con polvo `Fx_MegarachneDust`; daño **2** a quien esté a <1,8 u del impacto (una vez). Queda en el suelo **1,5 s vulnerable** (`Idle`/`Land`), luego `Climb` al cable más cercano. Mientras está en el suelo recibe daño; en cable no.
4. **Ráfaga:** desde el cable, `SilkShoot`; **6 bolitas** `Proj_MegarachneSilk` en abanico de ~30° hacia el jugador, daño **1**, desaparecen en pared; cada una deja una telaraña de suelo (`Fx_MegarachneWeb_Appear/Idle`) que **frena un 40 % durante 8 s** a jugador y enemigos que la pisen (usa el sistema de telarañas del piso 3 si ya existe, ver Araña de las frondas / tarea 04; si no, componente simple). Las telarañas se queman con fuego (usa la reacción de fuego existente; `Fx_MegarachneWeb_Burn`).
5. **Red:** aviso 1,2 s con `Fx_MegarachneNet_Shadow` que crece sobre el jugador (~3 u de diámetro; no sigue al jugador en los últimos 0,4 s), cae (`Fx_MegarachneNet_Fall`), si el jugador está dentro: **1 de daño y atrapado 1 s** (`Fx_MegarachneNet_Trapped`, sin poder moverse; sí disparar) y la red se rompe (`Break`). Reutiliza la mecánica de inmovilizar existente si la hay (telaraña/estado de enredo).
6. Teclas en la escena de prueba (evitar F1-F3 de Ctrl+Mayús ya usadas por M1): Ctrl+Mayús+F4 salto, F5 ráfaga, F6 red.
7. Pruebas PlayMode: en cable invulnerable; tras salto vulnerable ~1,5 s y vuelve a cable; salto hace 2 de daño en radio; ráfaga crea 6 proyectiles y deja telarañas que frenan; red atrapa 1 s con 1 de daño; ciclo rota los tres ataques; nunca persigue por el suelo.

## Números (cerrados con la usuaria, 2026-11-15)
Ciclo 4 s; salto aviso 1 s, ventana 1,5 s, daño 2; ráfaga 6 bolitas, daño 1, telaraña -40 % velocidad 8 s; red aviso 1,2 s, atrapa 1 s, daño 1. Radios y velocidad de bolitas PROVISIONALES (bolita ~10 u/s). Los tiempos se ajustarán al probar.

## Archivos que puede tocar
`MegarachneBoss*.cs`, `Assets/_Primordia/Editor/*Megarachne*` (importar efectos nuevos: Proj, Fx_MegarachneWeb/Net/Drop/Dust), pruebas, escena de prueba. Arte solo importar.

## Fuera de alcance
Saco y crías, fuego en cables, enfado, entrada, barra, muerte, recompensa, sonido, enganche a Floor_3. No ejecutar generadores `.py`.

## Cómo probar
`Test_BossMegarachne`: ella corre por los cables y alterna salto/ráfaga/red; solo se le puede dañar tras el salto. Lanzar `MegarachneBossTests`.

## Resultado (lo rellena Codex)
- Hecho:
- Archivos tocados:
- Pruebas:
- Dudas:
