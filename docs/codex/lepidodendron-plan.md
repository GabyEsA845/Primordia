# Lepidodendron ancestral — plan de tareas para Codex (escrito 2026-11-11 por Claude)

Jefe secreto del piso 3. Arte terminado y aprobado (`docs/multimedia-temp/floor3-bosses/lepidodendron/README.md`, fuente de verdad del arte y de las medidas). Diseño del combate: `README` sección «Combate» y `docs/memoria-claude/primordia-boss-lepidodendron.md` / `primordia-floor3-design.md` (línea del jefe secreto). Se trabaja **igual que con la Meganeura**: una tarea por subpaso, rama `codex/lepido-N-*` desde `feature/floor-3`, un menú `Primordia > Boss > ...`, escena de prueba `Test_BossLepidodendron` con teclas de prueba, pruebas PlayMode, y Claude revisa + la usuaria prueba antes de fusionar. **Cada tarea se escribe cuando la anterior está fusionada** (los números de combate se cierran con la usuaria justo antes).

| Tarea | Qué | Estado |
|---|---|---|
| 05 · **L1** | Sala (pasillo 116×18 + arena 40×26), árbol, capa de hojas, luz, sombra, decorados, 3 paredes sólidas, escena de prueba y teclas de teletransporte | HECHA y fusionada (2026-11-11) |
| 06 · **L2** | **Paredes**: vida (25 % del jefe cada una, dañar pared = dañar jefe), 4 estados de daño, nudos ×2 que revientan, fuego (frente + carbonizado + más daño), retirada / desmoronamiento en ceniza, cicatrices; **oleadas** por pared (1: Calamites y sapos; 2: arañas y bombarderos cuyo cono prende la pared; 3: mezcla + libélulas), odres y helechos junto a cada pared, esporas en arco sobre las paredes | por escribir |
| 07 · **L3** | **Fase final (25 %)**: 3 conos con armadura (aviso de vetas → baja → golpe → romper armadura, fuego la quema, cono roto no vuelve), tronco −80 %, sacudida de la copa, metano prendido junto al tronco baja los conos | por escribir |
| 08 · **L4** | **Ataques**: latigazos de raíces (aviso, surco), raíces que brotan bajo el jugador (corro de púas), lluvia de esporas con marca, enemigos por las esquinas | por escribir |
| 09 · **L5** | **Cierre**: barra de vida propia (nudos 75/50/25, conos colgando), presentación «Corazón del bosque» con sello «JEFE SECRETO», desplome (cinemática ~6,6 s, tronco tendido = pared norte), tocón + sala interior, recompensa (+1 corazón, Espora morada, reliquia Raíz ancestral), cómo se engancha al piso (puerta secreta) | por escribir |

## Cosas a cerrar con la usuaria ANTES de escribir la L2 (no inventar)
- Vida total del jefe y daño de cada ataque (provisional hasta ahora: 1000).
- Cuánto tarda en romperse una pared a golpes y con fuego; multiplicador del carbonizado; daño que recibe un nudo (ya: ×2).
- Tamaño y ritmo de las oleadas (cuántos enemigos, cada cuánto) y topes por sala.
- Si el jefe usa el `BossDamageLimiter` como la Meganeura.
- Reliquia Raíz ancestral: efecto (en discusión) y +1 corazón permanente; la llave del piso 4.

## Reglas comunes (como con la Meganeura)
- Siempre menú `Primordia` para lo de editor; escena `Test_BossLepidodendron` que se rehace con `Crear Lepidodendron y escena de prueba`.
- Cada tarea añade teclas de prueba (bajar la vida, saltar a la fase, teletransportes) y las documenta en su `Resultado`.
- Pruebas por tarea en `LepidodendronBossTests`; antes de dar por buena una tarea, `MeganeuraBossTests` + `BossHealthBarTests` siguen en verde.
- Constantes de combate al inicio del archivo, nombradas y marcadas provisionales.
