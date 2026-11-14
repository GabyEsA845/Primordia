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
| B | F1: sala del claro, espesura/cresta/trama, brecha, asomar, embestida con aviso, odres, fuego-mecha y placas abiertas, paso al 70 % | pendiente |
| C | F2: sala 60×33, ondas, árboles y troncos, helechos, aturdimiento, fuego busca lodo | pendiente |
| D | F3: anillo con hueco, apretón, placas por tramos, fuego en círculo | pendiente |
| E | Rueda del 10 %, muerte y cuerpo-escenario | pendiente |
| F | Barra (BossBarSkin), flechas de borde, presentación «rodeo y asomo»; luego sonido (paso 8) | pendiente |

Pendiente de cerrar al llegar a cada subpaso: tiempos de ciclo de ataques, tamaño del hueco del cerco, velocidades.
