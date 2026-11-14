# Arthropleura — plan de subpasos (escrito 2026-11-13 por Claude; la programa Claude, Codex sin tokens)

Jefe principal del piso 3. Arte aprobado: `docs/multimedia-temp/floor3-bosses/arthropleura/README.md` (fuente de verdad de hojas, filas, pivotes y cuadros que cuentan). Diseño: `docs/memoria-claude/primordia-floor3-design.md` (líneas de la Arthropleura). Rama `feature/boss-arthropleura` desde `feature/floor-3`; menú `Primordia > Boss > ...`; escena `Test_BossArthropleura` con teclas de prueba; pruebas PlayMode; la usuaria prueba cada subpaso y solo entonces se hace commit.

## Números CERRADOS con la usuaria (2026-11-13)
- Vida total **600**, repartida por los umbrales del diseño (muescas de la barra 70 % / 35 % / 10 %): **F1 100→70 % (180)**, **F2 70→35 % (210)**, **F3 35→10 % (150)**, **rueda último 10 % (60)**.
- Caparazón invulnerable todo el combate; solo recibe daño en **placas abiertas**. Daño por `BossDamageLimiter` (mismos topes que la Meganeura).
- Daño al jugador: **embestida 2, ola 1, apretón del cerco 2, contacto con el cuerpo 1**.
- F1/F2: el **fuego abre las placas** del tramo que arde: ~6 s ardiendo = abierto y vulnerable; luego `Charred` y cierra. El fuego pasa de pieza en pieza como mecha (retardo ~0,4 s por pieza).
- F3: se abren solas por **tramos de 2-3 piezas** (carne rosada que late, flecha rosa en el borde).

## Del diseño (ya decidido antes)
- Cadena: cabeza + 28 piezas + cola, paso 2,6 u (≈ 78 u), y de pantalla ×0,82, dibujo de cola a cabeza, Walk desfasado 1 cuadro por pieza.
- F1: claro cerrado 28×16 u con espesura ~6 u, cámara ×1,3; el cuerpo rodea por fuera (cresta `CanopyRidge`, trama), revienta el borde (`EdgeBreak`, brecha ≥ 5 u) y asoma/embiste. Solo fuego daña; odres de metano que vuelven a crecer.
- 70 %: derriba la pared (`WallBreak`), sala F2 60×33 u, cámara ×1 (o ×1,5 si se ve mal).
- F2: cuerpo-pared, ondas de pisotón (3 arcos de ~140° con 0,3 s), derriba árboles (Sigillaria; si embiste uno queda aturdido), troncos caídos = cobertura, come helechos (ardiendo se quema), fuego en mecha; ardiendo busca charcas de lodo.
- 35 %: F3 con cámara ×1,5; anillo que se cierra con un hueco (flechas, Patas de salto); si te pilla dentro: apretón (daño) y expulsión, luego reabre; fuego en círculo si el jugador prende el anillo.
- Último 10 %: rueda de ~8 u que rebota en paredes; daño al chocar (aturdida) y el fuego también le daña.
- Muerte: Head_Death, cadena en espiral, piezas `Dead` de cola a cabeza, humo; el cuerpo queda como escenario.

## Subpasos
| | Qué | Estado |
|---|---|---|
| A | Importar arte (menú), cadena de 30 piezas con seguimiento de cabeza, 16 direcciones, Walk desfasado, vida/fases/umbrales, escena `Test_BossArthropleura` | HECHO (2026-11-13), 4 pruebas en verde; falta que la usuaria lo pruebe (sin cámara alejada ni combate) |
| B1 | F1 (movimiento): claro 28×16 con espesura de tiles de pared, cresta de frondas sobre el cuerpo escondido, brecha, ciclo rodear 8 s → asomar → aviso 1,2 s → embestir → retirarse, daño al jugador (contacto 1, embestida 2), cámara ×1,3 | HECHO y APROBADO (2026-11-14), con aviso bajo la espesura, embestida recta que atraviesa el claro (2 agujeros) y pared invisible en los agujeros; 7 pruebas |
| B2 | F1 (fuego): odres de metano que rebrotan a los 20 s, fuego en mecha, placas abiertas ~6 s, daño al jefe 1,5 PV/s por pieza ardiendo y abierta, paso al 70 % (disparador) | tarea 11 escrita (`codex/arthro-b2-fuego`), pendiente de Codex |
| C1 | F2 (transición y ataques): al 70 % la espesura revienta en cadena con el jugador invulnerable (3,2 s), cámara ×1, la cabeza persigue al jugador y alterna pisotón (3 ondas en arco de 140°, daño 1) y embestida larga con aviso | PROGRAMADO 2026-11-14 (28 pruebas en verde); falta prueba de la usuaria |
| C2a | F2 (fuego y escenario): el fuego prende y daña en F2 hasta el 35 % (210 PV; mismas reglas que F1), 6 odres por la sala (4 del claro + 2), 6 helechos que la cabeza come al pasar (si arden, se enciende la cabeza; rebrotan a 25 s) | PROGRAMADO 2026-11-14 (31 pruebas); falta prueba de la usuaria |
| C2b-1 | Lagos de lodo profundo (2) y apagar el fuego: pieza ardiendo que pisa lodo se apaga; con 5+ ardiendo la cabeza corre al lago y da vueltas | PROGRAMADO 2026-11-14; falta prueba |
| C2b-2 | 3 Sigillarias que la embestida tumba (aturdido 3 s, tronco tumbado = obstáculo y cobertura de las ondas) | PROGRAMADO 2026-11-14 (37 pruebas); falta prueba de la usuaria |
| D1 | F3 (transición, cerco con hueco y apretón, cámara ×1,5) | PROGRAMADO 2026-11-14; falta prueba |
| D2 | F3 (placas por tramos que laten, daño hasta el 10 %, fuego en F3 con mecha circular, flechas de borde del punto débil) | pendiente |
| E | Rueda del 10 %, muerte y cuerpo-escenario | pendiente |
| F | Barra (BossBarSkin), flechas de borde, presentación «rodeo y asomo»; luego sonido (paso 8) | pendiente |

Pendiente de cerrar al llegar a cada subpaso: tiempos de ciclo de ataques, tamaño del hueco del cerco, velocidades.
