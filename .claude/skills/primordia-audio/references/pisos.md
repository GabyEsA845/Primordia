# Identidad sonora por piso

Lo común a todos los pisos está en `SKILL.md`. Aquí va lo propio de cada uno: modo, tempos, instrumentos,
ambiente y paleta de sus criaturas. Al cerrar el audio de un piso nuevo, añadir su sección (la misma estructura).
La idea general: **cada piso suena más grande y más hondo que el anterior**, igual que las criaturas crecen
(célula → océano → pantano…), pero con la misma familia de timbres chiptune orgánicos.

## Común a todo el juego

- **Interfaz** (`SFX_UI_*`): blips de burbuja agudos y secos en **re menor**; suenan también en pausa. No cambian
  de un piso a otro.
- **Jugador base** (`SFX_Player_*`): tonos medios y limpios, "verdes": burbujas, chasquidos, pops.
- **Tienda:** `Music_Shop` es la misma en todas las tiendas del juego (decidido por el usuario el 2026-10-27).
- Menú, victoria, derrota y fragmentos de linaje/jefe: los del piso 1 valen para todo el juego mientras no se pida
  otra cosa.
- Volúmenes de referencia (`vol`): frecuentes 0,25-0,4; normales 0,5-0,6; daño al jugador y muertes 0,9; ambientes
  0,25-0,3; música 0,6-0,7.

## Piso 1: sopa primordial (microscópico)

- **Tonalidad:** re menor / re dórico (`D_MINOR`); la tienda gira en fa mayor (mismas notas) y la ameba gigante en
  re frigio (`D_PHRYGIAN`, con mi bemol).
- **Tempos:** exploración y combate 100 BPM, 32 compases de 4/4 (las dos capas iguales); menú 72; tienda 144 en
  3/4 (vals); virus 140; ameba 90; escena final de la ameba 144, 30,0 s exactos; fragmentos a 120.
- **Instrumentos:** campanas FM (`i_bell`) para la melodía, pads de sierra filtrada, bajo redondo (`i_bass`),
  percusión de gotas (`drop`) y shaker, cama de burbujas (`bubble_bed`). Combate: batería chiptune (`make_kit`),
  bajo más rítmico y arpegios rápidos.
- **Ambiente:** `Amb_Floor1_Loop` (agua en calma, burbujas sueltas, rumor grave) y `Amb_BossRoom_Loop` (más denso,
  zumbido de infección).
- **Paleta de criaturas:** enemigos graves y viscosos (bacteria con chirrido, ameba que se hincha y gotea); virus
  eléctrico y cristalino (cápside); ameba gigante de gelatina enorme (golpes sordos, "boing", "SPLOTCH").
- Encargo completo: `PROMPT.md`. Pistas: `musica.py`; efectos: `efectos.py`.

## Piso 2: océano cámbrico (cerrado y en el juego, 2026-10-27)

Lo que ya está decidido o hecho:
- **Tema:** fondo marino en **tres franjas de profundidad** según el avance (Someras → Arrecife → Abismo), con
  nieve marina más densa cuanto más hondo; pasillos que mezclan franjas. Criaturas a escala 24 px (más grandes que
  la célula): el mundo suena más grande.
- **Ambiente ya hecho:** `Amb_Floor2_Loop` (en `efectos.py`, se oye junto a la fosa de la sala de paso y en todo el
  piso): mar abierto, rumor muy grave con oleaje lento (3 vaivenes por bucle de 24 s), corrientes lejanas y un fondo
  que respira sobre **mi** (41,2 / 61,8 / 82,6 Hz: mi y si graves). Mi y si natural caben en re dórico; en un
  tramo en re frigio (mi bemol) comprobar que el zumbido no choque.
- **Puertas:** losas de roca que chocan con polvo y burbujas al cerrar; al abrir, el sello se enciende y se retiran
  (sustituyen a la membrana del piso 1).
- **Enemigos:** trilobite (embiste tras aviso de 0,6 s, queda panza arriba), medusa azul (anillo de 8 cada 3 s),
  Ottoia (gusano bajo la arena: onda y círculo de 0,8 s antes de morder), esponja Vauxia (cura y da escudo), cardumen
  de Pikaia (5 en formación). Paleta: más quitina, roca y agua grande que gelatina.
- **Jefes:** Anomalocaris (sala oscura con plantas luminosas, zarpazos con aviso, escena de solo sus ojos, fase
  frenética) y Euriptérido (cueva que se derrumba, muda, barrido de cola, último aliento con bullet hell).

Decidido con el usuario (2026-10-27):
- Tonalidad en re, como todo el juego.
- **Música distinta por franja** (Someras, Arrecife, Abismo), cada una con **capas de exploración y combate**
  sincronizadas como en el piso 1.
- **Música nueva para la sala de cambio** (el laboratorio de Darwin con la cápsula), como se hizo con la tienda.

Cerrado y aprobado por el usuario (en `musica_piso2.py`, `efectos_piso2.py` e `instrumentos_piso2.py`):
- **Instrumentos:** marimba de conchas, canto de las profundidades, pads que ondulan, armónica de cristal,
  percusión de mar (conchas, quitina, roca) y eco largo; chiptune discreto en las capas de combate.
- **Franjas** (capas exploración/combate sincronizadas; el `MusicDirector` cambia de franja con un fundido cruzado
  de 3 s): Someras re dórico 100 BPM, Arrecife re eólico 108, Abismo re frigio 80 (con su ambiente
  `Amb_Floor2_Abyss_Loop`).
- **Sala de cambio:** `Music_Exchange`, re menor en 6/8, común a todos los pisos. La tienda sigue con `Music_Shop`.
- **Jefes** (rehechos más intensos a petición del usuario): Anomalocaris re frigio 126 BPM y frenesí a 140;
  Euriptérido re menor 120 y último aliento de 25,0 s exactos (8 + 8 + 9 s) que empieza con la primera salva.
  Sala de jefe con `Amb_Floor2_BossRoom_Loop`.
- **Efectos:** 102 + 2 ambientes. Avisos con la duración exacta del aviso del juego; los que el juego puede acortar
  (carga del frenesí, aviso de pinzas en fase blanda, cerco, silueta) se cortan con `Sfx.Stop`.
