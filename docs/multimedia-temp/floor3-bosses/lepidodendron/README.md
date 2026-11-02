# Lepidodendron ancestral (jefe secreto del piso 3)

Árbol milenario al final de una sala horizontal de ~5 pantallas (Bosque humeante). **Concepto C** (idea del usuario,
aprobado el 2026-11-01): en la arena final se ve el **tronco gigante subiendo hasta perderse en una capa de hojas fija
en pantalla**; la copa no se ve nunca. Morado = camino secreto. Todo hecho con la skill `primordia-sprites`; sin
importar todavía (va con la rama del piso 3). **Arte terminado** (6l-6p).

## Combate (resumen para el código)

- **Sala:** pasillo de 116×18 u con **3 paredes de raíces** (x = 34 / 64 / 94, de 3 / 3,5 / 4 u, 25 % de vida cada
  una; dañar la pared = dañar al jefe; el fuego la rompe mucho antes; nudos morados ×2; oleadas por pared) y la
  **arena final** de 40×26 u. La sombra moteada de la copa empieza en la pared 1 y se espesa hasta la arena.
- **Arena:** cámara alejada ×0,75 (40×22,5 u). Tronco en el centro, **2 u al norte del centro** (pie en arena +10,5 u
  desde la pared norte). **No se pasa por detrás**: las raíces de atrás se meten en la pared norte, así que el tronco
  va siempre **detrás del jugador** (recorte translúcido solo como red de seguridad en los lados del pie).
- **Fase final (25 %):** los **3 conos** están arriba, fuera de la vista, envueltos en una **armadura de ramas**. Aviso
  (las vetas suben a la copa + sombra que crece + hojas que caen) → una rama baja el cono y **golpea**; se queda un rato
  en el suelo: romper la armadura (el fuego la quema antes) y dañar el cono. Cono roto = no vuelve a atacar; ×3. Si no
  se rompe a tiempo, sube y la armadura vuelve a crecer, pero el daño se mantiene. El tronco no recibe daño en esta
  fase. **Metano prendido junto al tronco**: lo sacude y bajan de golpe los conos que queden. Además: latigazos de
  raíces desde los lados, raíces que brotan bajo el jugador, esporas que llueven de la copa, enemigos por las esquinas.
- **Capa de hojas:** se mece suave con el viento; **sacudida violenta** en el ataque de los conos (y con el metano).
  2 huecos con rayos de luz.
- **Muerte:** la copa se rasga y entra la luz, el tronco se parte y queda un **tocón hueco**: la entrada a la sala
  de la recompensa (+1 corazón, Espora morada, reliquia Raíz ancestral en un pedestal dorado).

## Tronco, pie y vetas (6l-1: `generar_lepidodendron.py piezas` → `Tree/`)

**Pivote de todas las piezas = el pie del tronco** (centro de la base). Una pieza por hoja, sin animación salvo las vetas.

| Hoja | Cuadros | Celda | ms | Pivote (px) | Uso |
|---|---|---|---|---|---|
| Lepido_Trunk | 1 | 288×384 | — | (144, 352) | Tronco: 11 u en el pie → ~8 u arriba, 22 u de alto; se oscurece y arriba se disuelve en trama (lo tapa la capa de hojas). Pie en 5 lóbulos. Siempre detrás del jugador |
| Lepido_Veins_Rest | 1 | 288×384 | — | (144, 352) | Capa encima del tronco: 3 vetas tenues (reposo) |
| Lepido_Veins_Warn | 8 | 288×384 | 70 | (144, 352) | **Aviso** antes de bajar un cono: el brillo sube del pie y se mete en la copa en el cuadro 8 (después, la sacudida y la sombra del cono) |
| Lepido_Veins_Hit | 2 | 288×384 | 60 | (144, 352) | Golpe: destello y se apaga (luego vuelve a Rest) |
| Lepido_Mound | 1 | 480×230 | — | (240, 96) | Suelo: montículo que se funde con el suelo (borde en trama) + raíces Stigmaria medio enterradas que salen de cada lóbulo. Debajo del tronco y del jugador |
| Lepido_MoundShadow | 1 | 480×230 | — | (240, 96) | Sombra de contacto translúcida (alfa 90), debajo del montículo |
| Lepido_BackRoots | 1 | 520×210 | — | (260, 196) | Suelo: las 2 raíces de atrás que se meten en la pared norte (la pared las tapa arriba). Su colisión cierra el paso por detrás |

Orden de dibujo: BackRoots → MoundShadow → Mound → (sombra moteada) → Trunk → Veins → jugador y enemigos → rayos de
luz → capa de hojas.

## Capa de hojas, luz y sombra (6l-2: `generar_lepidodendron.py copa` → `Canopy/`)

La capa de hojas va **fija arriba de la pantalla** (en vertical) y se desplaza **con el mundo en horizontal**; se
dibuja encima de todo. Las hojas y esporas que caen son **solo adorno**.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| Canopy_Arena_Sway | 8 | 704×112 | 150 | Arena (40 u + 32 px de margen por lado). Bucle: lo que cuelga se mece con el viento. La pieza va **10 px por encima** del borde de la pantalla, centrada en la arena. 2 huecos de luz en x = 176 y 512 px de la pieza (y = 40) |
| Canopy_Arena_Shake | 6 | 704×112 | 60 | **Sacudida violenta** (ataque de los conos, metano junto al tronco): toda la masa tiembla y lo que cuelga se agita; en bucle mientras dure (~1 s) y vuelve a Sway |
| Canopy_StripThin_Sway | 8 | 256×80 | 150 | Pasillo **desde la pared 2**: tira fina en mosaico (encaja consigo misma en horizontal) |
| Canopy_StripMid_Sway | 8 | 256×80 | 150 | Pasillo **desde la pared 3**: tira media en mosaico |
| Fx_Dapple | 8 | 256×256 | 300 | Sombra moteada en mosaico (encaja por los 4 lados), translúcida: el viento la mueve despacio. Por todo el suelo desde la pared 1; el código le sube la opacidad de 0 (pared 1) a 1 (arena) |
| Fx_LightShaft | 8 | 118×248 | 140 | Rayo de luz translúcido con motas que flotan; el hueco está en (19, 0) de la celda; baja en diagonal hacia la derecha hasta el suelo (~15 u). Uno por hueco |
| Fx_LightPool | 8 | 52×21 | 140 | Charco de luz en el suelo, al pie de cada rayo (~4,2 u a la derecha y 15 u por debajo del hueco) |
| Fx_FallingLeaf | 6 | 16×16 | 70 | Hoja de aguja que cae girando (adorno): ~70 en la sacudida, caen a 1,6-3,2 px por cuadro de juego con un vaivén lateral |
| Fx_FallingSpore | 3 | 6×6 | 120 | Espora que cae (adorno), ~30 % de lo que cae |

Escena: `vista_previa_6l2_escena_copa.png` + `gif/escenas/Lepido_copa.gif` (vaivén → sacudida con hojas → calma);
el pasillo: `vista_previa_6l2_pasillo.png`.

## Decorados del pie (6l-3: `generar_pie_lepidodendron.py` → `Decor/`)

Solo adorno, **sin colisión**. Pivote = abajo en el centro (salvo los hongos en repisa: el centro). Todas las celdas
llevan 2 px de margen (las lianas no: nacen del borde de arriba). La colocación de la escena está en `DECOR_LAYOUT`
(px respecto al pie del tronco) por si sirve de guía.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| Lepido_Fungus_S / _M / _L | 1 | 34×28 / 42×35 / 50×42 | — | Hongos en repisa, **encima del tronco** (pegados a los lóbulos) |
| Lepido_Shroom{A,B,C}_Idle | 6 | 28×24 | 160 | Setas moradas con halo: laten despacio (bucle) |
| Lepido_Shroom{A,B,C}_Warn | 4 | 28×24 | 70 | **Se encienden con el aviso de las vetas** (a la vez que Lepido_Veins_Warn; bucle mientras dure) |
| Lepido_Shroom{A,B,C}_Hit | 2 | 28×24 | 60 | Destellan con el golpe al árbol |
| Lepido_Flowerlets{A,B,C} | 4 | 24×18 | 200 | Florecillas moradas en racimos bajos (no se recogen; distintas de Flower_Purple). Se mecen 1 px |
| Critter_Roach_Run / _Idle | 4 / 2 | 20×16 | 70 / 300 | Cucaracha primitiva (mira a la derecha; Flip X): huye corriendo cuando el jugador se acerca |
| Critter_Millipede_Crawl | 4 | 28×12 | 120 | Milpiés pequeño que recorre las raíces despacio |
| Critter_Dragonfly_Fly / _Perch | 2 / 1 | 20×20 | 50 / — | Libelulita: posada; vuela a otro sitio si el jugador se acerca o en la sacudida |
| Critter_Springtail_Hop | 4 | 12×12 | 90 | Colémbolos: saltitos en el musgo (2-3 juntos, desfasados) |
| Lepido_DarkPuddle | 4 | 52×22 | 180 | Charco oscuro entre lóbulos con el reflejo tembloroso de un rayo (suelo) |
| Lepido_Liana{A,B}_Sway | 8 | 24×144 | 150 | Lianas que bajan de la copa **pegadas al tronco** (encima del tronco), al ritmo del vaivén de la copa |
| Remains_Pulmonoscorpius | 1 | 68×44 | — | **Casi enterrado** (como todos los restos: bulto de tierra con la forma del cuerpo y borde en trama, va sobre el montículo): asoman la cola curvada y una pinza del escorpión gigante |
| Remains_PaleoWings | 1 | 52×28 | — | Asoma la punta de un ala de paleodictíptero (Mazothairos), con manchas |
| Remains_Trigonotarbid | 1 | 24×20 | — | Asoman el vientre y las patas encogidas de un trigonotárbido panza arriba |
| Remains_AmphibianSkull | 1 | 42×32 | — | Asoma la parte de arriba del cráneo de un anfibio primitivo (Proterogyrinus), con las órbitas |

Escena: `vista_previa_6l3_pie.png` (reposo y aviso a ×2) + `gif/escenas/Lepido_pie.gif`.

## Paredes de raíces (6m: `generar_paredes_lepidodendron.py piezas escenas` → `Walls/`)

Cada pared es **una masa sólida de raíces trenzadas**: un modelo completo hecho con un mapa de alturas proyectado en
picada (no capas). Cruza el pasillo (18 u) de norte a sur: por el norte se apoya en la pared de arriba, por el sur se
hunde en el suelo y por los lados baja y se funde con él. Paleta oliva del árbol (pared 1 más verde y húmeda, 3 más
oscura con musgo). **Pivote = centro del borde sur del pasillo** (la pared ocupa 18 u por encima).

| Pared | Grosor | Nudos | Celda | Pivote | Nudos: recorte (x, y, lado) en la celda |
|---|---|---|---|---|---|
| 1 | 3 u | 2 | 124×349 | (62, 325) | (44, 71, 38) · (39, 191, 38) |
| 2 | 3,5 u | 3 | 132×356 | (66, 332) | (41, 41, 41) · (47, 125, 41) · (44, 217, 41) |
| 3 | 4 u | 4 | 140×365 | (70, 341) | (42, 27, 47) · (50, 92, 47) · (41, 159, 47) · (49, 235, 47) |

Hojas de cada pared `n` (todas con la celda de la pared, salvo las de los nudos):

| Hoja | Cuadros | ms | Uso |
|---|---|---|---|
| Lepido_Wall{n}_D0 … D3 | 1 | — | Los **4 estados de daño** (intacta → agrietada → rota → a punto de caer): huecos oscuros entre cordones rotos con astillas en el borde, y grietas. Pasar de uno a otro cada 25 % de la vida de la pared |
| Lepido_Wall{n}_Charred_D0 … D3 | 1 | — | **Carbonizada** en cada estado (ascuas en grietas y roturas). El fuego avanza por la pared (~3 s desde donde prende, hacia los dos lados): el código va cambiando D → Charred por franjas horizontales detrás del frente; en el frente, llamas `Fx_GroundFire` (floor3-mutations/Effects) a lo ancho, **con su base justo en la línea donde la pared pasa a carbonizada** (así van sincronizadas hacia el norte y hacia el sur); detrás, `Fx_RootSmolder`. Lo carbonizado recibe más daño |
| Lepido_Wall{n}_Hurt_D0 … D3 | 2 | 60 | Golpe: silueta blanca y luego roja del estado actual |
| Lepido_Wall{n}_Knot{i}_Idle | 4 | 160 | Capa encima del nudo i: la luz morada de sus grietas late (bucle) |
| Lepido_Wall{n}_Knot{i}_Hit | 2 | 60 | Golpe al nudo (×2 de daño): las grietas destellan |
| Lepido_Wall{n}_Knot{i}_Wound / _WoundCharred | 1 | — | Parche opaco (se pone encima de la pared) cuando el nudo **revienta**: un hueco oscuro con restos de savia. Las roturas y grietas esquivan los nudos, así que el parche vale en cualquier estado de daño |
| Fx_KnotBurst | 6 | 70 | 64×64, centrado en el nudo: destello, salpicadura de savia morada y astillas que caen (al reventar; luego Wound) |
| Lepido_Wall{n}_Retract | 8 | 90 | A cero, **si no está totalmente carbonizada** (quedan raíces vivas: el árbol las recoge): **se retira bajo tierra** (antes, ~0,3 s de sacudida moviendo el sprite ±2 px): se encoge, se estrecha y salta tierra |
| Lepido_Wall{n}_Scar | 1 | — | La cicatriz que queda: el suelo de la sala con **tierra revuelta** (terrones sombreados, hoyos alargados donde estaban las raíces, algún muñón y poca savia). Los terrones van apareciendo durante la retirada. Se puede pisar |
| Lepido_Wall{n}_Crumble | 8 | 90 | A cero, **solo si está totalmente carbonizada** (raíces muertas, no se pueden mover): **se desmorona en ceniza** por manchas, con ascuas |
| Lepido_Wall{n}_AshScar | 1 | — | El montón bajo de ceniza que queda. Se puede pisar |
| Fx_RootSmolder | 6 | 100 | 24×24: rescoldo (chispas y humo) sobre lo carbonizado, en bucle |

Láminas: `vista_previa_6m_muestra.png` (las 3 paredes), `vista_previa_6m_estados.png` (pared 2: daño, carbonizada,
retirada, ceniza), `vista_previa_6m_escenas.png` + `gif/escenas/Lepido_pared_{nudo,fuego,retirada,ceniza}.gif`.

## Conos con armadura (6n-1: `generar_conos_lepidodendron.py piezas escenas` → `Cones/`)

Cono alargado de escamas moradas envuelto en 5 ramas de corteza en espiral (la armadura), colgado de una rama gruesa
que baja de la copa. **Celda 72×104 y pivote = la base del cono (36, 98)** salvo donde se indica. La rama de la celda
llega hasta su borde de arriba y se alarga hasta la copa repitiendo `Lepido_ConeBranch_Tile` encima (la capa de hojas
tapa dónde nace). Solo arde la armadura (los lóbulos del manguito incluidos); la rama está viva y no se quema.

**Flujo:** aviso (Veins_Warn + `Fx_ConeShadow` que crece donde caerá + sacudida de la copa) → baja acelerando →
**golpe** al llegar al suelo (rebote de 2 px hecho en código, `Fx_ConeSlam`, queda `Fx_ConeCrater`; mientras está en el
suelo, `Fx_ConeShadow` cuadro 4 como sombra de contacto) → ventana en el suelo: cada golpe a la armadura = `Hurt` +
`Fx_ArmorChip` y pasa de estado 0 → 1 → 2 → `Shatter`; el fuego la quema (`Burn{s}` → `ArmorCharred{s}` +
`Fx_RootSmolder`), y carbonizada recibe más daño → cono expuesto (`Exposed_Idle`; cada golpe al cono es daño al jefe:
`Exposed_Hurt` + `Lepido_Veins_Hit`) → al llegar a cero, `Burst`: queda `Husk` en el suelo (no vuelve a atacar) y la
rama sube sola (`ConeBranch_Torn`). Si la ventana acaba sin romper la armadura: tiembla, `Fx_ConeLift` y sube. Si
**no está quemada**, sube con `Regrow{s}` y la próxima vez baja intacta; si **está quemada**, la madera está muerta y
**no rebrota**: sube y vuelve a bajar con el mismo `ArmorCharred{s}` (usar el fuego nunca se pierde). El daño del cono
se mantiene siempre. **Cono en el suelo y cono roto:** colisión solo en la base, orden por altura con el jugador y
**translúcido mientras el jugador está detrás** (como el pie del tronco). Los conos rotos se quedan en el suelo hasta el
final: se ve cuántos quedan.

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| Lepido_ConeBranch_Tile | 1 | 72×24 | — | Tramo de rama que se repite en vertical (encaja consigo mismo y con el borde de arriba de las celdas del cono) |
| Lepido_ConeBranch_Torn | 1 | 72×104 | — | La rama sin cono tras el reventón (manguito desgarrado, fibras, savia morada); sube sola |
| Lepido_Cone_Armor{0,1,2} | 1 | 72×104 | — | Armadura intacta / astillada / rota |
| Lepido_Cone_ArmorCharred{0,1,2} | 1 | 72×104 | — | Lo mismo, carbonizada (ascuas); recibe más daño |
| Lepido_Cone_Armor{s}_Hurt, ArmorCharred{s}_Hurt | 2 | 72×104 | 60 | Golpe: silueta blanca y luego roja |
| Lepido_Cone_Burn{0,1,2} | 8 | 72×104 | 150 | Arde: lo carbonizado sube desde abajo con el frente de llamas (`Fx_GroundFire` del piso 3, ya en la hoja). Termina en ArmorCharred{s} |
| Lepido_Cone_Shatter / _ShatterCharred | 8 | 128×112, pivote (64, 104) | 90 | Último golpe a la armadura rota: el cono se enciende por dentro y las ramas salen a trozos, caen y se deshacen. Termina en Exposed_Idle cuadro 0. **Cuadro que cuenta: 0** (el cono ya recibe daño desde el 1) |
| Lepido_Cone_Exposed_Idle | 6 | 72×104 | 120 | Cono expuesto: las escamas se abren y la luz de dentro late (bucle) |
| Lepido_Cone_Exposed_Hurt | 2 | 72×104 | 60 | Golpe al cono expuesto |
| Lepido_Cone_Burst | 10 | 128×120, pivote (64, 114) | 80 | Revienta: se hincha (0-2), **estalla (cuadro 3)** en una nube de esporas lila (adorno, sin daño) y escamas; acaba en Husk + ConeBranch_Torn |
| Lepido_Cone_Husk | 1 | 72×104 | — | El cono roto que se queda en el suelo, apagado y partido (sin rama) |
| Lepido_Cone_Regrow{1,2} | 8 | 72×104 | 100 | Sube sin romperse y **sin quemar**: ramas tiernas bajan del manguito en espiral y la armadura vieja se deshace. Termina en Armor0. Se reproduce mientras sube |
| Fx_ConeShadow | 8 | 72×36, centro | 90 | Aviso: la sombra crece donde caerá el cono (cuadro 7 al tocar el suelo; el 4 sirve de sombra de contacto). Translúcida |
| Fx_ConeSlam | 7 | 128×64, pivote (64, 36) | 70 | Golpe: estallido de polvo, onda que se abre (picada), terrones y esporas. **Cuadro que cuenta: 0** (daño en el sitio); la onda empuja hasta el 2. Las filas de arriba del pivote van detrás del cono y las de abajo delante |
| Fx_ConeCrater | 1 | 72×32, centro | — | Marca del golpe en el suelo (hoyo, borde levantado, grietas); el código la desvanece cuando el cono sube |
| Fx_ConeLift | 6 | 64×48, pivote (32, 40) | 70 | Terrones y polvo al despegarse del suelo |
| Fx_ArmorChip / _ArmorChipCharred | 5 | 40×40, centro | 50 | Golpe a la armadura: astillas de corteza (o carbón y ascuas), en el punto del golpe |

Láminas: `vista_previa_6n1_muestra.png`, `vista_previa_6n1_estados.png`, `vista_previa_6n1_animaciones.png`,
`vista_previa_6n1_escena_{romper,fuego}.png` + `gif/escenas/Lepido_cono_{romper,fuego}.gif`.

## Ataques de la fase final (6n-2: `generar_ataques_lepidodendron.py piezas escenas` → `Attacks/`)

Las raíces de los ataques usan la paleta oliva de las paredes, trenzado irregular con musgo, tierra pegada y espinas,
y grietas sueltas de luz morada. Todo lo que toca el suelo se funde con él (labios de tierra, bordes en trama).

**Latigazo** (barrido en franja desde un lado de la arena). Celda 352×56, **pivote (0, 34) = el borde de la arena**
en la línea de la franja; se dibuja saliendo por la izquierda y la derecha es **Flip X**. Alcanza ~20 u (hasta el
centro). Orden: va con el jugador por altura (base = pivote + 6 px).

| Hoja | Cuadros | ms | Uso |
|---|---|---|---|
| Lepido_Whip_Warn | 8 | 100 | Aviso (~0,8 s, va en el suelo): grietas con luz morada que corren por la franja desde el borde (llegan al centro en el cuadro 5) y polvo cerca del frente; los 2 últimos laten |
| Lepido_Whip_Sweep | 23 | 55 | El suelo se abomba y revienta junto al borde (0-1) y sale la raíz (2); **barre** con una **ola de tierra** que se abomba y revienta delante de la punta, levantando dos caballones de terrones entre los que solo asoma el lomo de la raíz (3-12, frenando al final); se queda (13-14); **se recoge deslizándose por el surco** (15-20) y el montículo se cierra (21-22). **Cuadros con daño: 2-20** (también mientras se recoge). Punta (x en px desde el pivote) por cuadro: —, 14, 32, 84, 132, 175, 214, 247, 276, 299, 317, 329, 334, 334, 334, 292, 236, 172, 108, 52, 22, —, —; la raíz ocupa desde x = 10 hasta la punta, ~1,3 u de ancho. La copa se sacude en 3-14 |
| Lepido_Whip_Furrow | 1 | — | El surco que queda (= último cuadro de Sweep): **tierra revuelta** (zanja irregular, terrones, piedrecitas, trozos de raíz, caballones a los lados). Solo adorno, el código lo desvanece en ~1 s |

**Corro de puntas** (raíces bajo el jugador). Celda 80×96, **pivote (40, 78) = el centro del corro en el suelo**
(donde estaba el jugador al empezar el aviso). Radio de daño ~1,3 u.

| Hoja | Cuadros | ms | Uso |
|---|---|---|---|
| Lepido_Spikes_Warn | 6 | 130 | Aviso (~0,8 s, en el suelo): grietas en círculo con luz morada y un brillo tenue que crece |
| Lepido_Spikes_Erupt | 8 | 80 | Estallido de tierra y **primero la púa central** (0), luego las 4 de alrededor abriéndose hacia fuera (1-2), se quedan (3-4), se hunden (5-6) y queda tierra revuelta (7). **Cuadros con daño: 0-4**. Va con el jugador por altura |
| Lepido_Spikes_Scar | 1 | — | Tierra revuelta (= último cuadro de Erupt): adorno, el código la desvanece en ~0,6 s |

**Lluvia de esporas lila.**

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| Proj_LepidoSpore | 4 | 12×24, pivote (6, 18) = centro de la espora | 100 | Espora que cae (núcleo oscuro, halo lila que late) con su estela corta de motitas encima. Se dibuja encima de todo salvo la copa |
| Fx_LepidoSporeMark | 6 | 34×22, centro | — | **Marca de aviso** donde caerá, con el lenguaje de la piedra del piso 2 (`Fx_RockFallShadow`): sombra oscura que crece, halo rojizo y **borde rojo discontinuo** (rojo de aviso `(255, 80, 60)`) que gira y se cierra hacia la sombra; motitas lila que ya caen. El código elige el cuadro según lo que le falta a la espora (0 = recién sale, 5 = a punto de tocar el suelo: el borde destella en blanco) |
| Fx_LepidoSporeSplash | 5 | 24×16, pivote (12, 11) | 60 | Al tocar el suelo: **daño solo en ese momento** (cuadro 0), salpicadura de polvo lila y desaparece |

Láminas: `vista_previa_6n2_muestra.png`, `vista_previa_6n2_piezas.png`, `vista_previa_6n2_escenas.png` +
`gif/escenas/Lepido_{latigazo,corro,esporas}.gif`.

## Desplome (6o-1: `generar_desplome_lepidodendron.py escena` → `Death/`)

Al morir (idea del usuario): las vetas parpadean hasta apagarse; la copa se rasga encima del tronco y **se forma una
grieta de derecha a izquierda mientras el árbol se inclina**; el tronco se parte a 4,5 u del suelo, cae **recto hacia
la entrada** girando sobre la bisagra de la izquierda, resbala del tocón y queda tendido **en lugar de la pared norte**
(34 u, entero a la izquierda del tocón); su copa revienta la pared pasada la pared de raíces 3. **El mapa queda así el
resto de la partida.** Del tocón (macizo) se abre una **grieta con luz dorada en la cara sur**: el jugador se acerca y
**pulsa E** para entrar en la sala de dentro. El morado del árbol se apaga; solo el tocón late tenue.

**Cinemática (~6,6 s, el jugador quieto):** 10 cuadros de vetas → 5 de grieta (lenta) → 12 de caída (**arranca casi
suspendida y acelera**, ~1,8 s: un árbol tan grande cae despacio) → 3 de golpe pesado → ~5 de polvo → la cámara vuelve
en ~0,9 s mientras se abre la grieta. **Cámara:** quieta en la arena hasta que el tronco baja (α ≈ 48°, cuadro 7 de la
caída); luego lo acompaña hasta el golpe (borde izquierdo de la cámara = pie − 50 u) y temblor (+5 / −3 px); al final
vuelve a la arena. **Aviso: solo la sombra del tronco** (sin borde ni señal). Escena: `vista_previa_6o1_escena.png` + `gif/escenas/Lepido_desplome.gif`.

Posiciones respecto al **pie del tronco** (el pivote de Lepido_Trunk). Orden de dibujo: suelo (sombra, escombros) →
tocón → tronco que cae / tendido (por encima del tocón) → efectos → jugador → columna de luz → capa de hojas (por sus
huecos, que muestran el cielo, se ve el tronco delante mientras cae).

| Hoja | Cuadros | Celda | ms | Pivote / posición | Uso |
|---|---|---|---|---|---|
| Lepido_Death_Veins | 10 | 288×384 | 70 | (144, 352) | Capa sobre Lepido_Trunk: las vetas parpadean y se apagan (el último cuadro, vacío) |
| Lepido_Death_Crack | 5 | 288×384 | 180 | (144, 352) | **Sustituye a Lepido_Trunk**: el árbol entero inclinándose hasta 5° con la grieta en cuña que se abre de derecha a izquierda |
| Lepido_Death_Fall | 15 | 1056×416 | 220, 200, 180, 170, 160, 150, 140, 130, 120, 110, 100, 90, 100, 130, 180 | (944, 352) | El tronco suelto con su copa: cae despacio y acelera (0-11, de 7° a 87°, resbala del tocón al final) y **golpea** (12-14, rebote pesado). **Cuadro que cuenta: 12** (daño en la franja de la sombra). A la vez: tocón = Lepido_Stump_Break |
| Lepido_FallenTrunk | 1 | 1056×416 | — | (944, 352) | El tronco tendido para siempre (= último cuadro de Fall). **Colisión:** de pie − 40,2 u a pie − 6,2 u en x, centrado a pie − 4,5 u en y, grueso ±4,8 u (en la rotura) a ±2,9 u (junto a la copa); la copa, sin colisión salvo sobre la pared |
| Lepido_Stump_Break | 15 | 288×384 | (con Fall) | (144, 352) | El tocón macizo mientras cae el tronco: la grieta de la cara sur se va formando |
| Lepido_Stump_Open | 8 | 288×384 | 110 | (144, 352) | La grieta termina y se abre ligeramente: hilo de luz dorada que se derrama en el suelo |
| Lepido_Stump_Idle | 6 | 288×384 | 160 | (144, 352) | Bucle: la luz respira y los bordes laten morado tenue. **Interacción: pulsar E junto a la grieta** (radio ~2 u delante del pie) |
| Canopy_Arena_Tear | 17 | 704×112 | (con Crack + Fall) | como Canopy_Arena_Sway | La copa se rasga encima del tronco (0-4, mientras cruje) y hacia la izquierda por donde pasa al caer (5-16), con sacudida |
| Canopy_Arena_Torn_Sway | 8 | 704×112 | 150 | como Canopy_Arena_Sway | La copa rasgada para siempre (gran hueco sobre el tocón) |
| Fx_FallShadow | 17 | 848×176 | (con Crack + Fall) | esquina arriba-izquierda en pie + (−54 u, −10,5 u) | Aviso (solo sombra, sin borde): la **sombra del tronco** se alarga hacia la entrada a medida que se inclina (lo que cubre en planta) y se oscurece al acercarse al suelo; al final, la mancha de la copa con huecos de luz. Se quita al golpear |
| Fx_TrunkSplinters | 6 | 160×112 | 60 | centro en pie + (0, −4,5 u) | Astillas al soltarse el tronco (cuadros 0-5 de Fall) |
| Fx_CrownCrash | 8 | 320×256 | 70 → 100 | centro en pie + (−43,2 u, −5,5 u) | La copa revienta la pared: piedras, polvo y hojas (desde el cuadro 7 de Fall) |
| Fx_TrunkDust | 8 | 880×224 | 70 → 100 | esquina arriba-izquierda en pie + (−56 u, −12 u) | Polvo a ras de suelo por el borde sur del tronco y motas de esporas moradas (ambiente) |
| Lepido_WallRubble | 1 | 448×128 | — | centro en pie + (−43,2 u, −7,5 u) | Escombros permanentes de la pared norte reventada |
| Fx_GoldColumn | 6 | 128×176 | 120 | abajo en el centro, en el pie + (0, 4 px) | Columna de luz dorada por el rasgón sobre el tocón (aparece con Stump_Open y se queda), translúcida |
| Lepido_Shroom{A,B,C}_Off | 1 | 24×20 | — | como las setas | Setas del pie apagadas (grises) tras la muerte |

Láminas: `vista_previa_6o1_escala_{sala,arena}.png` (composición), `vista_previa_6o1_muestra.png`,
`vista_previa_6o1_piezas.png`, `vista_previa_6o1_escena.png`.

## Recompensa (6o-2: `generar_recompensa_lepidodendron.py escenas` → `Reward/`)

**En la arena (tras el desplome):** por la columna de luz bajan despacio **el corazón** (`Item_HeartContainer_Idle` de
la tienda + `Fx_HeartGlow` detrás) **y la Espora morada** (`Item_PurpleSpore_Fall`, gira), ~1,8 s con un vaivén
lateral, y se quedan **flotando** delante del tocón: el corazón en pie + (−22, +26) px y la espora en pie + (+22, +26)
px, con `Fx_RewardShadow` debajo (en pie + (±22, +32)). **Se cogen al tocarlos.** La espora lanza su **presentación
pequeña** («OBJETO CLAVE»). La grieta del tocón: **pulsar E** → **fundido dorado** (`#e0a234` → la sala, ~0,45 s; al
salir, al revés).

**La sala de dentro del tocón** (una pantalla, 480×270, mágica: más grande por dentro): `Lepido_StumpRoom` es el fondo
fijo; el suelo es un **círculo de 92 px de radio centrado en (240, 158)** (colisión: la pared, todo lo de fuera). La
rana aparece en (240, 244) mirando al norte; **la salida** (pulsar E) es la grieta en (240, 252). Luz: el pedestal
(`Fx_StumpRoom_Light`, se quita al coger la reliquia) y rayos por las rendijas (`Fx_StumpRoom_Rays`, encima de todo).
Setas que laten (`Lepido_Shroom{A,B,C}_Idle` del pie, abajo en el centro de su celda) en: A (178, 95), B (209, 75),
C (266, 74), A (294, 88), B (315, 111), C (162, 116).

| Hoja | Cuadros | Celda | ms | Pivote / posición | Uso |
|---|---|---|---|---|---|
| Item_PurpleSpore_Idle | 6 | 16×24 | 140 | abajo en el centro | Espora morada flotando: las vetas laten, vaivén de 1 px, halo morado |
| Item_PurpleSpore_Fall | 6 | 16×24 | 100 | abajo en el centro | Gira al bajar por la luz (se estrecha y vuelve) |
| Item_PurpleSpore_Big | 6 | 38×53 | 140 | centro | Para la presentación |
| Fx_RewardShadow | 6 | 16×6 | 140 | centro | Sombrita bajo lo que flota (crece y mengua con el vaivén) |
| Fx_HeartGlow | 4 | 24×24 | 150 | centro | Brillo dorado detrás del corazón que flota |
| Lepido_StumpRoom | 1 | 480×270 | — | esquina (0, 0) | La sala: suelo con los anillos del árbol, pared de fibras, raíces que cuelgan |
| Fx_StumpRoom_Rays | 8 | 480×270 | 150 | esquina (0, 0) | Rayos finos por las rendijas del techo, con motas (translúcido, encima) |
| Fx_StumpRoom_Light | 1 | 480×270 | — | esquina (0, 0) | Luz dorada del pedestal en el suelo (translúcida); se apaga con Pedestal_Fade |
| Lepido_StumpRoom_Exit | 6 | 24×20 | 140 | centro en (240, 252) | La grieta de salida: hilo de luz que late |
| Lepido_Pedestal_Lit | 4 | 56×52 | 160 | (28, 48) en (240, 164) | Pedestal con la reliquia: raíces enroscadas, copa de ámbar y oro que brilla |
| Lepido_Pedestal_Fade | 6 | 56×52 | 100 | (28, 48) | Al coger la reliquia: el brillo residual se apaga |
| Lepido_Pedestal_Empty | 1 | 56×52 | — | (28, 48) | El cuenco vacío (para siempre) |
| Relic_RootAncestral_Idle | 8 | 24×32 | 120 | esquina en pedestal + (−12, −61) | La reliquia **flota y gira** sobre la copa (veta morada, punta que brilla). **Coger: pulsar E** junto al pedestal |
| Relic_RootAncestral_Big | 8 | 48×60 | 120 | centro | Para la presentación |
| Icon_PurpleSpore_16 / _32, Icon_RootAncestral_16 / _32 | 1 | 16×16 / 32×32 | — | centro | Iconos de la pausa (fila de Reliquias y de objetos clave) |

**Presentación de reliquias y objetos clave** (formato común, distinto de la de los jefes; sobre la pantalla de
referencia 480×270; el juego se para): ~0,5 s de entrada (la pantalla se atenúa a negro morado `(6, 4, 10)` con alfa
175, el halo aparece y **el objeto sube desde donde estaba hasta el centro** (240, 98) ya en su versión `_Big`), el texto
aparece en ~0,3 s, se queda hasta pulsar E (o ~2,5 s) y se va al revés. Al terminar con la reliquia, el pedestal hace
`Pedestal_Fade` → `Empty`.

| Pieza | Celda | Posición (480×270) | Uso |
|---|---|---|---|
| UI_RelicHalo | 120×120, 8 cuadros, 120 ms | centro en (240, 98) | Halo de rayos dorados que gira despacio (translúcido) |
| Etiqueta | texto | centrada, arriba en y = 28 | «R E L I Q U I A» u «O B J E T O   C L A V E» en **Silkscreen 8**, color `#ffd468` |
| Title_Relic_RootAncestral / Title_Key_PurpleSpore | 182×35 / 184×27 | centrado, arriba en y = 166 | El nombre en **Jersey 10** (32 px) con relleno dorado en 4 bandas, contorno y sombra |
| UI_RelicFillet_Top / _Bottom | 244×9 | centrados, arriba en y = 156 / justo debajo del nombre | Filetes dorados; el de arriba con un adorno de raíz en el centro |
| Efecto | texto | centrado, 14 px por debajo del nombre | «Al llegar a un piso nuevo recuperas toda la vida» / «Abre un camino secreto en el piso 4» en Silkscreen 8, crema `(246, 232, 200)` |

Escenas: `vista_previa_6o2_escenas.png` + `gif/escenas/Lepido_recompensa_{bajada,sala}.gif`; muestra y hojas:
`vista_previa_6o2_{muestra,piezas}.png`.

## Barra de vida (6p: `generar_barra_lepidodendron.py` → `BossBar/UI_BossBar_Lepidodendron_*`)

Sistema BossBarSkin, **abajo y centrada como las de los jefes**. El marco es una **rama del árbol** (corteza de rombos);
la vida es la **savia** que corre por una hendidura abierta a lo largo de la rama: **ventana de 260×8 en (46, 13)** del
marco de **320×44**, rellena de izquierda a derecha (Image *Filled*, horizontal). A la izquierda, un trozo del tronco
con sus vetas hace de «cabeza». Las **3 muescas son nudos morados** en el **75 / 50 / 25 %** (x = 241 / 176 / 111 del
marco): cada uno revienta cuando cae su pared. En la **fase final (25 %)** cambian el marco, la cabeza y la savia, y del
último tramo **cuelgan los 3 conos** con su estado.

| Hoja | Cuadros | Celda | ms | Posición en el marco | Uso |
|---|---|---|---|---|---|
| `Frame` / `Frame_Final` | 1 | 320×44 | — | (0, 0) | La rama con la hendidura y el penacho de la punta; Final con grietas de luz dorada y morada |
| `Head_Calm` | 4 | 46×44 | 160 | (0, 0) | El tronco con las vetas que laten despacio (paredes) |
| `Head_Final` | 3 | 46×44 | 90 | (0, 0) | Vetas moradas y doradas que laten deprisa (fase de los conos) |
| `Head_Hit` | 2 | 46×44 | 60 | (0, 0) | Destello de las vetas al recibir daño |
| `Back` | 1 | 260×8 | — | (46, 13) | Fondo de la hendidura |
| `Fill_Sap` | 4 | 260×8 | 150 | (46, 13) | Savia morada que late despacio (una gota más clara corre hacia la cabeza) |
| `Fill_SapGold` | 3 | 260×8 | 90 | (46, 13) | Fase final: savia morada con brillo dorado que late deprisa |
| `Fill_Trail` | 1 | 260×8 | — | (46, 13) | Barra fantasma (espera ~0,4 s y se vacía) |
| `Knot` | 4 | 12×20 | 160 | (muesca − 6, 7) | Nudo de la muesca: la luz morada de sus grietas late |
| `Knot_Hit` | 2 | 12×20 | 60 | (muesca − 6, 7) | Destella cuando se golpea un nudo de la pared en curso |
| `Knot_Broken` | 1 | 12×20 | — | (muesca − 6, 7) | La herida que queda (hueco oscuro con savia que gotea) |
| `Fx_KnotBurst` | 6 | 32×40 | 70 | (muesca − 16, −3) | El nudo revienta al caer su pared: destello, savia morada y astillas |
| `Cone_Appear` | 4 | 12×20 | 90 | (x − 6, 24), x = 62 / 80 / 98 | Al llegar al 25 %, los 3 conos bajan de la rama |
| `Cone_Armor` / `Cone_Charred` / `Cone_Broken` | 1 | 12×20 | — | igual | Acorazado / armadura quemada / roto (apagado y partido) |
| `Cone_Exposed` | 3 | 12×20 | 120 | igual | Expuesto: las escamas laten morado |

Vista: `vista_previa_6p_barra.png` y `gif/Lepido_BossBar_combate.gif` (la barra a lo largo del combate).

## Presentación (6p: `../../boss-intros/generar_presentaciones_piso3.py solo Lepidodendron`)

Retrato `boss-intros/Portraits/Portrait_Lepidodendron.png` (176×176: el tronco en contrapicado con todas las vetas
encendidas subiendo a una copa espesa y oscura), nombre en dos líneas
`Titles/Title_Lepidodendron.png` («LEPIDODENDRON» a 40 px y «ANCESTRAL» a 28 px), título **«Corazón del bosque»** y
sello **«JEFE SECRETO»** morado junto al título (la banda es algo más alta para las dos líneas). Maqueta: `boss-intros/maqueta_rotulo_lepidodendron.png`.

**Entrada (sin hojas nuevas):** al entrar en la sala, la cámara **recorre el pasillo** (se ven las 3 paredes) hasta el
árbol (~1,5 s); allí `Lepido_Veins_Warn` (las vetas se encienden de abajo arriba), `Canopy_Arena_Shake` (la copa se
sacude) y los nudos de las 3 paredes destellan a la vez (`Lepido_Wall{n}_Knot{i}_Hit`) con un temblor de ±1 px (se
estremecen); sale el rótulo y la cámara vuelve a la rana.

## Láminas y escenas

- `generar_escala_lepidodendron.py [A] [B] [C]`: escalas. A y B (vistas descartadas: árbol exento en picada / pared
  frontal) y **C, la aprobada**: `vista_previa_escala_{sala,final}_C.png`.
- `generar_lepidodendron.py muestra`: `vista_previa_6l1_muestra.png` (arena ×2 y pie ×3), `vista_previa_6l1_vetas.png`.
- `generar_lepidodendron.py escena`: `vista_previa_6l1_escena_aviso.png` + `gif/escenas/Lepido_aviso.gif` (reposo,
  aviso, sacudida provisional, golpe). GIF de las vetas en `gif/`.

Regenerar: `python generar_lepidodendron.py muestra piezas escena copa` , `python generar_pie_lepidodendron.py` y `python generar_paredes_lepidodendron.py muestra piezas escenas` y `python generar_conos_lepidodendron.py muestra escenas`, `python generar_ataques_lepidodendron.py muestra escenas`, `python generar_desplome_lepidodendron.py escala muestra escena` y `python generar_recompensa_lepidodendron.py muestra escenas` y `python generar_barra_lepidodendron.py`.
