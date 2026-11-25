# Encargo: música y efectos de sonido de Primordia (piso 3, pantano carbonífero)

Tercer encargo de audio, con el mismo sistema, formatos y reglas que los del piso 1 (`PROMPT.md`, «Formato y entrega»)
y el piso 2 (`PROMPT_piso2.md`). Se genera con `generar_audio.py`, desde `efectos_piso3.py` (y más adelante
`musica_piso3.py`). Este encargo empieza con el ambiente del pantano, que hace falta para la sala de paso del piso 2
al 3; el resto del audio del piso (música, Bocas, enemigos, reacciones y jefes) se añade en el paso 8 del PLAN.

## Estilo sonoro (lo decidido hasta ahora)

- **Aire caliente y húmedo, vida por todas partes**, frente al mar abierto del piso 2: anfibios que croan a lo
  lejos, insectos que pasan, lodo que burbujea y gotas que caen del musgo. Más adelante, fuego, metano y veneno
  (reacciones del piso).
- **Tonalidad común del juego: re.** El fondo del ambiente respira sobre **re y la** (73,4 / 110 / 36,7 Hz), con un
  batido lento que lo hace húmedo y cerrado. En el fundido de la sala de paso no choca con el mi del piso 2.
- **Tres tramos del incendio que avanza** (pantano verde, bosque humeante, tierra quemada): de momento solo hay
  ambiente del pantano verde; los de los otros dos tramos van con el resto del audio del piso.

## Ambiente

| Archivo | Uso | Duración | Notas |
|---------|-----|----------|-------|
| `SFX_Amb_Floor3_Loop.wav` | Sube junto a la charca de la sala de paso del piso 2 (fundido por distancia) y suena debajo de la música en el pantano verde (bucle, muy bajo) | 24 s | Aire pesado que respira, fondo en re y la, croar lejano en grupos de 2-3 (graves, re y la), insectos que pasan de un lado a otro, burbujas gordas de lodo y metano con un «blop» grave, gotas del musgo y frondas mecidas. Sin ritmo |

## Efectos de sonido

En cada tabla, "Cuándo suena" dice qué lo dispara (entre paréntesis, el script o evento del código). La usuaria
decidió (2026-11-24) crear **todos los sonidos nuevos posibles** en lugar de reutilizar los de pisos anteriores.

### Reacciones y terreno

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Fire_Ignite` | 3 | 0,35 s | Algo prende: suelo, charco, telaraña, maleza (`GroundFire` al nacer) | Soplo que se enciende («fuf») con chispas; seco y corto |
| `SFX_Fire_Burn_Loop` | 1 | 4 s | Bucle mientras haya fuego en el suelo cerca (`GroundFire`; uno solo aunque haya varios) | Crepitar de ramitas y un rugido grave y suave de llama; sin ritmo |
| `SFX_Fire_Extinguish` | 2 | 0,6 s | Un fuego se apaga con agua, lodo o lluvia (`PlayerMudSink` apaga al jugador, tormenta) | Siseo de vapor que baja y una gota |
| `SFX_Status_Burn_Start` | 2 | 0,4 s | El jugador o un enemigo empieza a arder (`Burn`, `PlayerBurn`) | Llamarada pequeña que envuelve, con chisporroteo |
| `SFX_Status_Burn_Tick` | 3 | 0,08 s | Cada daño de quemadura (frecuente) | Chasquido de brasa, muy corto |
| `SFX_Methane_Explosion` | 3 | 0,9 s | Gas + cualquier ataque: odre, nube del Saco de metano, burbuja (`Explosion`) | «Bum» de gas: golpe grave hueco, soplo de fuego y crepitar que cae |
| `SFX_Methane_FogFlash` | 1 | 1,2 s | La niebla de metano prendida: el fogonazo la recorre (`MethaneMist`) | Llama que corre de un lado a otro, un rugido que crece y se va |
| `SFX_Methane_Mist_Loop` | 1 | 6 s | Bucle muy bajo cerca de la niebla de metano (`MethaneMist`) | Siseo húmedo y lento, burbujitas de gas |
| `SFX_Methane_GasBag_Burst` | 2 | 0,3 s | Se rompe la membrana de un odre (`MethaneSac`), justo antes de la explosión | Membrana que se rasga con un soplo de gas |
| `SFX_Methane_GasBag_Regrow` | 1 | 0,8 s | El odre vuelve a crecer (`MethaneSac`) | Hinchado húmedo que sube de tono |
| `SFX_Methane_Bubble_Warn` | 1 | 1,0 s | La burbuja de metano del Burbujeo avisa (`MethaneBubbler`, 1 s) | Burbujeo grave que se acelera y sube hasta el final |
| `SFX_Methane_Bubble_Pop` | 2 | 0,5 s | La burbuja revienta y lanza gas (`MethaneBubbler`) | «Blop» enorme con salpicadura de lodo y soplo |
| `SFX_Poison_Puddle` | 3 | 0,25 s | Sale un charco de veneno (vainas, Escupitajo, muerte de envenenado; `PoisonPuddle`) | Salpicadura viscosa con efervescencia |
| `SFX_Poison_ToxicCloud` | 1 | 1,0 s | Fuego + veneno: sube la columna mostaza (`ToxicCloud`) | Siseo que sube como columna, burbujeo ácido y un soplo grave |
| `SFX_Poison_Bulb_Burst` | 2 | 0,45 s | La planta venenosa revienta al golpearla (`PoisonBulb`) | Bulbo carnoso que estalla con chorro y salpicadura |
| `SFX_Poison_Bulb_Regrow` | 1 | 0,8 s | El bulbo vuelve a crecer (`PoisonBulb`, a los 25 s) | Crujido vegetal suave que se hincha |
| `SFX_Fumarole_Warn` | 1 | 1,0 s | La fumarola avisa (`SteamVent`, 1 s) | Retumbo del suelo y gorgoteo que suben hasta el final |
| `SFX_Fumarole_Erupt` | 2 | 0,8 s | Sale la columna (`SteamVent`) | Chorro de vapor y fuego: rugido con siseo, empuja |
| `SFX_Fern_Rustle` | 3 | 0,25 s | Se cruza un helecho (`Fern`) | Frondas que rozan, suaves |
| `SFX_Fern_Burn` | 2 | 0,9 s | Un helecho arde y el fuego se propaga (`Fern`) | Llamarada de hojas secas que crepitan fuerte y se apagan |
| `SFX_Brush_BurnAway` | 1 | 1,2 s | La maleza quemable se deshace y abre el paso (`BrushWall`) | Ramas que crujen, se desmoronan en ceniza con un suspiro |
| `SFX_Mud_Sink_Loop` | 1 | 2 s | Bucle mientras el jugador se hunde en el lodo (`PlayerMudSink`) | Succión espesa y burbujas lentas |
| `SFX_Mud_Escape` | 2 | 0,4 s | Sale del lodo esquivando (`PlayerMudSink`) | «Plop» de succión que suelta |
| `SFX_Mud_Drown` | 1 | 0,9 s | Hundido del todo (−½ corazón) y vuelve a tierra (`PlayerMudSink`) | Trago de lodo grave, burbujas que suben y un golpe sordo |
| `SFX_Mud_Splash` | 3 | 0,15 s | Pisar o salpicar lodo (aterrizajes, caídas) | Chapoteo espeso y grave |
| `SFX_Water_Splash` | 3 | 0,15 s | Pisar o salpicar agua de las charcas | Chapoteo claro con 2-3 gotas |
| `SFX_Web_Stick` | 2 | 0,25 s | Quedarse pegado en una telaraña (`SpiderWeb`) | Hilo pegajoso que se estira con un chasquido elástico |
| `SFX_Web_Burn` | 2 | 0,5 s | Una telaraña arde (`SpiderWeb`) | Chisporroteo fino y rápido que se apaga |

### Jugador: esquivas de los cuerpos del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Player_Dodge_Frog` | 3 | 0,25 s | Esquiva de la rana venenosa (Área): salto plano (sustituye a `Player_Dodge` en ese cuerpo) | Muelle húmedo de patas que empujan: «boing» corto y blando que sube |
| `SFX_Player_Dodge_Diplocaulus` | 3 | 0,3 s | Esquiva del *Diplocaulus* (cuerpo a cuerpo): ondulación | Coletazo que ondula: soplo que va y vuelve con un roce de piel mojada |
| `SFX_Player_Dodge_Scorpion` | 3 | 0,35 s | Esquiva del vinagrillo (proyectil): bola que rueda | Caparazón que se enrolla con un chasquido y rueda: tamborileo de quitina |

### Bocas del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Mouth_Spit_Shoot` | 3 | 0,2 s | Escupitajo venenoso: sale la bola (cada 2,8 s) | Garganta que se hincha y escupe: «ptú» húmedo con burbuja |
| `SFX_Mouth_Spit_Trail` | 3 | 0,08 s | La bola deja un charquito del rastro (cada 0,5 s; frecuente, muy bajo) | Gota viscosa que cae, apenas un toque |
| `SFX_Mouth_Spit_Burst` | 2 | 0,45 s | La bola revienta (pared, otra bola o al encoger del todo): 6 de daño y charco grande (`PoisonSpitBurst`) | Reventón de baba: golpe blando, salpicadura y efervescencia |
| `SFX_Mouth_Spit_Split` | 1 | 0,3 s | Camada (nivel III): la bola se divide en 4 | Cuatro «plips» casi juntos que se abren en cruz |
| `SFX_Mouth_Spin_Start` | 1 | 0,2 s | Peonza: empieza el giro al mantener el clic | Arranque que coge vueltas: soplo que sube |
| `SFX_Mouth_Spin_Loop` | 1 | 0,5 s | Bucle mientras se gira (hasta 3-4 s) | Zumbido de aire que gira, con un batido de 8 vueltas por segundo |
| `SFX_Mouth_Spin_Hit` | 3 | 0,08 s | Cada golpe del giro (cada 0,25 s, frecuente) | Palmetazo carnoso y seco |
| `SFX_Mouth_Spin_Dizzy` | 1 | 0,5 s | Mareo de 0,5 s al acabar el giro | Pajaritos: tres notas que se tambalean (re, la) |
| `SFX_Mouth_Spin_Deflect` | 2 | 0,12 s | El giro devuelve un proyectil (nivel II) | Rebote brillante y rápido |
| `SFX_Mouth_Spin_Boomerang` | 1 | 0,6 s | Bumerán (nivel III): el giro sale hacia el cursor y vuelve (`SpinBoomerang`) | Silbido que se aleja y vuelve, como un efecto doppler |
| `SFX_Mouth_Vinegar_Start` | 1 | 0,15 s | Chorro de vinagre: empieza el chorro | Apretón y siseo que se abre |
| `SFX_Mouth_Vinegar_Loop` | 1 | 1,0 s | Bucle mientras sale el chorro (`JetAttack`) | Rociado continuo, ácido y fino, con borboteo |
| `SFX_Mouth_Vinegar_Hit` | 3 | 0,06 s | Cada impacto del chorro (cada 0,15 s, frecuente) | Salpicón ácido muy corto |
| `SFX_Mouth_Vinegar_Empty` | 1 | 0,35 s | La barra se vacía (+1 s de recarga) | Chorro que tose y se queda sin líquido |
| `SFX_Mouth_Vinegar_Ready` | 1 | 0,25 s | La barra vuelve a estar llena | Gorgoteo corto que sube y un brillo (re) |
| `SFX_Mouth_Vinegar_Splash` | 2 | 0,25 s | Salpicadura (nivel III): estallido de radio 1 cada 0,5 s | Salpicadura ácida en abanico |

### Mutaciones del piso 3: Extremidades, Piel y Órganos

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Limbs_Jump_Takeoff` | 3 | 0,15 s | Patas de salto: despegue de la esquiva (`JumpingLegs`) | Patas que empujan: golpe elástico y soplo que sube |
| `SFX_Limbs_Jump_Land` | 3 | 0,2 s | Aterrizaje (3 de daño desde el nivel II) | Caída blanda y pesada; sobre lodo o agua suena además su chapoteo |
| `SFX_Limbs_Jump_Stun` | 1 | 0,4 s | Rana toro (nivel III): el aterrizaje aturde | Golpe de suelo con onda y un zumbido que tiembla |
| `SFX_Limbs_Spinner_Thread` | 2 | 0,3 s | Patas hilanderas: se tiende el hilo (`SilkThreads`) | Hilo que se estira y se tensa con un tañido |
| `SFX_Limbs_Spinner_Snare` | 1 | 0,3 s | Un enemigo toca el hilo y se frena | Hilo que vibra y se pega |
| `SFX_Limbs_Spinner_Web` | 1 | 0,5 s | Telaraña (nivel III): se forma en el cruce e inmoviliza (`SilkWeb`) | Varios hilos que se tensan a la vez y un acorde seco |
| `SFX_Skin_Mucus_Charged` | 1 | 0,4 s | Piel anfibia: el moco termina de cargarse (`AmphibianMucus`) | Capa húmeda que se hincha con un brillo suave |
| `SFX_Skin_Mucus_Absorb` | 2 | 0,3 s | El moco absorbe un golpe | Golpe que se hunde en gelatina, «blop» amortiguado |
| `SFX_Skin_Mucus_Reflect` | 1 | 0,3 s | Reflejo (nivel III): el moco devuelve el proyectil | Rebote elástico y húmedo |
| `SFX_Skin_Char_Kindle` | 1 | 0,4 s | Escamas carbonizadas: tras pisar fuego tus ataques queman 4 s (`CharredScales`) | Brasas que se avivan sobre la piel, con un siseo grave |
| `SFX_Skin_Char_Flare` | 1 | 0,6 s | Brasa (nivel III): al recibir un golpe estallas en llamas (radio 1,5) | Llamarada en anillo que se abre de golpe |
| `SFX_Organ_Methane_Release` | 1 | 0,6 s | Saco de metano: clic derecho, sale la nube (`MethaneCloud`) | Saco que se vacía: soplo de gas espeso con burbujeo |
| `SFX_Organ_Methane_Cloud_Loop` | 1 | 2,0 s | Bucle muy bajo mientras flota la nube (6 s) | Gas que borbotea y sisea suave |
| `SFX_Organ_Pheromone_Cast` | 1 | 0,6 s | Feromonas: clic derecho, nube en el cursor | Rociado dulce y un acorde que se tuerce (re con la bemol) |
| `SFX_Status_Confused` | 1 | 0,5 s | Un enemigo queda confundido y ataca a los suyos | Trino que oscila y se desafina |

### Especiales del ámbar (sala del ámbar)

Paleta propia: resina espesa que se endurece en cristal cálido; lo que lleva nota, en re (re, fa, la), con un brillo
dorado. Distinta del destello iridiscente de las especiales del piso 2.

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Room_AmberBlock_Break` | 1 | 1,0 s | Se rompe con E el bloque de ámbar de la sala y sale la burbuja con la especial (`AmberBlock`) | Ámbar grueso que cruje, se raja y estalla en esquirlas, con un acorde dorado al salir la burbuja |
| `SFX_Special_Cerci_Dash` | 3 | 0,15 s | Cercos de cucaracha: el quiebro (`CerciDodge`) | Carrerita de patas rapidísima y un soplo corto |
| `SFX_Special_Cerci_Ready` | 1 | 0,2 s | Vuelve a estar lista una carga del quiebro | Dos toques de antena y un brillo dorado |
| `SFX_Special_Capsule_Encase` | 1 | 0,5 s | Cápsula de ámbar: un golpe te deja a ≤½ corazón y la resina te encierra (`AmberCapsuleShield`) | Resina espesa que te envuelve y se endurece con un tintineo de cristal |
| `SFX_Special_Capsule_Heal` | 1 | 0,6 s | La cápsula te cura (½ corazón; más en II y III) | Acorde cálido en re a través del ámbar, como oído desde dentro |
| `SFX_Special_Capsule_Hit` | 2 | 0,1 s | Un golpe choca contra la cápsula mientras estás dentro | Toque sordo de cristal grueso, sin daño |
| `SFX_Special_Capsule_Shatter` | 2 | 0,5 s | Sales y la cápsula estalla en esquirlas (aturden desde el nivel II) | Cristal cálido que estalla y esquirlas que caen tintineando |
| `SFX_Special_Drop_Shoot` | 2 | 0,25 s | Gota de ámbar: clic derecho, sale la gota (`AmberDropShot`) | Gota de resina espesa lanzada con un «plop» meloso |
| `SFX_Special_Drop_Encase` | 2 | 0,45 s | La gota encierra a un enemigo en un bloque (`AmberCocoon`) | Resina que cubre y se cristaliza deprisa, con un crujido |
| `SFX_Special_Drop_Hit` | 3 | 0,1 s | El enemigo encerrado recibe daño o para un proyectil (hace de escudo) | Golpe seco en ámbar, algo más grave que el de la Cápsula |
| `SFX_Special_Drop_Shatter` | 2 | 0,5 s | El bloque se rompe y suelta esquirlas que dañan alrededor | Como `Capsule_Shatter`, más pequeño y con más esquirlas |
| `SFX_Special_Drop_Fossil` | 1 | 0,6 s | Nivel III: el enemigo encerrado muere al instante y queda fosilizado en ámbar | Crujido que se congela y un acorde grave que se apaga, como de piedra |
| `SFX_Special_Drop_Ready` | 1 | 0,25 s | La gota vuelve a estar lista (`AmberGlandGlow`) | Gota que se forma con un brillo dorado (re) |

### Clases del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Lineage_Parasite_Attach` | 3 | 0,15 s | Parásito: se pega uno a un enemigo (`Parasite`) | Ventosa húmeda que se agarra con un chupetón |
| `SFX_Lineage_Parasite_Suck` | 3 | 0,08 s | Cada daño de 1 por segundo de un parásito (frecuente, muy bajo) | Sorbo pequeño y blando |
| `SFX_Lineage_Parasite_Drain` | 1 | 0,5 s | Cada 15 de vida chupada te cura medio corazón (`ParasiteBenefit`) | Sorbo largo que sube y un latido cálido |
| `SFX_Lineage_Parasite_Jump` | 2 | 0,35 s | El parásito salta a otro enemigo al morir su huésped (0,35 s) | Se suelta con un chasquido y vuela con un silbido húmedo que cae |
| `SFX_Lineage_Colonize_Floor3` | 1 | 0,6 s | Hongo (piso 3): brotan las matas al empezar el combate (`ColonyBenefit`; sustituye a `Lineage_Colonize` en este piso) | Brotes que asoman con soplos de esporas y un brillo bioluminiscente que titila |
| `SFX_Lineage_Mushroom_Sprout` | 2 | 0,4 s | Un enemigo muere sobre una mata y brota otra grande junto a él | Brote que empuja la tierra, se abre con un «puf» y titila |

### Toques de los linajes del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Lineage_Eryops_Wade_Loop` | 1 | 1,0 s | Acecho del fango: bucle mientras cruzas el lodo medio sumergido (`EryopsTouch`) | Lodo que se aparta delante, espeso y rítmico como un cazador nadando |
| `SFX_Lineage_Eryops_Emerge` | 1 | 0,4 s | Sales del lodo con el impulso de +30 % de velocidad | Salida chorreando y terrones que caen |
| `SFX_Lineage_Xenacanthus_Spine` | 2 | 0,15 s | Espina cefálica: golpeas a un confundido que cuenta como herido | Pinchazo seco de espina de hueso |
| `SFX_Lineage_GasSting_Toxify` | 2 | 0,5 s | Aguijón de gas: el chorro vuelve tóxico el metano en lugar de explotarlo | El gas sisea, burbujea y se agria (sin explosión) |
| `SFX_Lineage_Centipede_Puddle` | 3 | 0,2 s | Cien patas: cada aterrizaje deja un charco de veneno (`CentipedeTouch`) | Carrerita de muchas patas que acaba en un charco |
| `SFX_Lineage_Tinder_Shoot` | 2 | 0,25 s | Yesca: la bola del Escupitajo sale como brasa de hongo (sustituye a `Mouth_Spit_Shoot`) | «Ptú» seco con chispas, sin baba |
| `SFX_Lineage_Tinder_Trail` | 3 | 0,1 s | Yesca: la brasa deja un trozo de rastro de fuego (frecuente, sustituye a `Mouth_Spit_Trail`) | Chasquido de brasa al tocar el suelo |
| `SFX_Lineage_Tinder_Burst` | 1 | 0,6 s | Yesca: la brasa revienta en brasas y deja fuego (sustituye a `Mouth_Spit_Burst`) | Estallido de yesca: «puf» de esporas que prende y chispas que llueven |
| `SFX_Lineage_Zombie_Rise` | 1 | 0,5 s | Zombi: el enemigo confundido se levanta como aliado (`ZombieAlly`, 0,5 s) | Crujido de cuerpo que se alza, hongos que brotan y un soplo de esporas |
| `SFX_Lineage_Zombie_Collapse` | 1 | 0,5 s | Zombi: cae de verdad al acabar sus 4 s | Cuerpo que se desmorona en polvo de esporas |
| `SFX_Lineage_Lamprey_Attach` | 2 | 0,2 s | Ventosa giratoria: girando, el parásito pega otro al enemigo más cercano (`LampreyTouch`) | Dos chupetones seguidos, el segundo más lejano |
| `SFX_Lineage_Leech_Burst` | 1 | 0,5 s | Charca de sanguijuelas: la bola revienta y saltan parásitos a los más cercanos (`LeechTouch`) | Reventón húmedo y varios gusanillos que salen con chupetones |

### Enemigos del piso 3

Paleta de enemigos: más graves que el jugador (80-800 Hz), con algo de suciedad. Plantas = madera, fibra y hojas;
insectos = quitina y zumbido; sapo = carne húmeda y croar. Golpe (`Hurt`) y muerte (`Death`) propios de cada enemigo
(campos `hurtSound` y `deathSound` de `EnemyData`).

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Enemy_Calamites_Hurt` | 3 | 0,12 s | La madre Calamites recibe un golpe | Golpe hueco de tallo articulado, como caña |
| `SFX_Enemy_Calamites_Death` | 1 | 0,8 s | Muere la madre | Caña que se quiebra por los nudos y cae con un susurro de hojas |
| `SFX_Enemy_Calamites_Sprout` | 2 | 0,35 s | Echa un retoño (cada 4 s mientras te ve) | Brote que rompe la tierra con un crujido y se estira |
| `SFX_Enemy_Calamites_SporeRing` | 1 | 0,3 s | Lanza el anillo de 10 esporas (cada 7 s) | Soplo seco en anillo: muchos «puf» casi juntos |
| `SFX_Enemy_CalamitesSprout_Hurt` | 2 | 0,08 s | Un retoño recibe un golpe | Chasquido de tallo tierno |
| `SFX_Enemy_CalamitesSprout_Death` | 2 | 0,25 s | Muere un retoño | Tallo que se parte, corto |
| `SFX_Enemy_CalamitesSprout_Wither` | 1 | 0,6 s | Muere la madre y los retoños se secan (una vez, por todos) | Hojas que se secan y crujen, un suspiro que baja |
| `SFX_Enemy_Medullosa_Hurt` | 3 | 0,12 s | La Medullosa recibe un golpe | Golpe en fronda carnosa, sordo |
| `SFX_Enemy_Medullosa_Death` | 1 | 0,7 s | Muere | Frondas que se desploman con un crujido de fibra |
| `SFX_Enemy_Medullosa_Launch` | 2 | 0,3 s | Dispara la vaina (marca el suelo donde estás) | Lanzamiento de mortero vegetal: «tunc» hueco |
| `SFX_Enemy_Medullosa_Fall` | 1 | 1,1 s | La vaina vuela hacia la marca (1,1 s; empieza con el disparo) | Silbido que baja y crece hasta el final, cuando cae |
| `SFX_Enemy_Medullosa_Impact` | 2 | 0,4 s | La vaina cae: medio corazón en radio 0,9 y charco de veneno (`MedullosaSeed`) | Vaina que revienta, golpe blando y salpicadura venenosa |
| `SFX_Enemy_Dragonfly_Buzz_Loop` | 1 | 1,0 s | Bucle mientras alguna libélula vuela en la sala | Zumbido de alas grandes, grave y batido (re) |
| `SFX_Enemy_Dragonfly_Warn` | 1 | 0,7 s | Marca la línea del picado (0,7 s) | El zumbido se tensa, sube y se fija al final |
| `SFX_Enemy_Dragonfly_Dive` | 2 | 0,4 s | Cae en picado a 14 u/s | Zumbido que pasa como un rayo, con soplo |
| `SFX_Enemy_Dragonfly_Land` | 2 | 0,27 s | Se posa (0,8 s vulnerable) | Patas que tocan el suelo y alas que se pliegan con un chasquido |
| `SFX_Enemy_Dragonfly_Hurt` | 3 | 0,1 s | Recibe un golpe (posada) | Quitina fina que cruje |
| `SFX_Enemy_Dragonfly_Death` | 1 | 0,6 s | Muere | Alas que zumban y se apagan, cuerpo que cae |
| `SFX_Enemy_Bombardier_Warn` | 1 | 0,6 s | Se gira y avisa (0,6 s) | Presión que sube en el abdomen: borboteo químico que hierve hasta el final |
| `SFX_Enemy_Bombardier_Flame_Start` | 1 | 0,15 s | Empieza el lanzallamas (fase de salida) | Detonación química seca, «pff» |
| `SFX_Enemy_Bombardier_Flame_Loop` | 1 | 1,0 s | Bucle mientras sale el lanzallamas (1,2 s) | Chorro de fuego a presión, con pulsos químicos rápidos |
| `SFX_Enemy_Bombardier_Flame_End` | 1 | 0,3 s | Acaba el lanzallamas (fase final) | El chorro se corta con un último pulso y chisporroteo |
| `SFX_Enemy_Bombardier_Hurt` | 3 | 0,1 s | Recibe un golpe (+50 % por la espalda) | Caparazón duro que suena hueco |
| `SFX_Enemy_Bombardier_Death` | 1 | 0,4 s | Muere | Caparazón que se raja y un último «pff» químico |
| `SFX_Enemy_Spider_FernShake_Loop` | 1 | 1,0 s | Bucle mientras la araña espera escondida en el helecho, solo cerca del jugador | Helecho que tiembla nervioso, con roces |
| `SFX_Enemy_Spider_Emerge` | 1 | 0,4 s | Sale del helecho | Frondas apartadas de golpe y patas que corren |
| `SFX_Enemy_Spider_Silk` | 2 | 0,3 s | Lanza seda en arco (`SpiderSilk`) | Chorro de seda que silba y se estira |
| `SFX_Enemy_Spider_Leap` | 2 | 0,3 s | Salta para apartarse o acercarse | Patas que empujan y caen, rápido |
| `SFX_Enemy_Spider_Hurt` | 3 | 0,1 s | Recibe un golpe | Chillido seco y quitina |
| `SFX_Enemy_Spider_Death` | 1 | 0,6 s | Muere | Patas que se encogen crujiendo y un chillido que se apaga |
| `SFX_Enemy_Toad_Croak` | 2 | 0,5 s | Ve al jugador (aviso, `alertSound`) | Croar grave de garganta hinchada (re) |
| `SFX_Enemy_Toad_Spit` | 2 | 0,3 s | Escupe una bola de lodo en arco | Gárgaras y escupitajo espeso |
| `SFX_Enemy_Toad_BallSplat` | 2 | 0,3 s | La bola cae y deja un charco que ralentiza (`ToadMudBall`) | Pegote de lodo que se aplasta |
| `SFX_Enemy_Toad_Dive` | 1 | 0,4 s | Se zambulle en el lodo (inmune 2 s) | Zambullida grave con burbujas |
| `SFX_Enemy_Toad_Surface` | 1 | 0,3 s | Vuelve a asomar | Burbuja que revienta y lodo que escurre |
| `SFX_Enemy_Toad_Swell` | 1 | 0,5 s | Se hincha para reventar (0,5 s) | Globo de carne que se infla y sube de tono hasta el final |
| `SFX_Enemy_Toad_Burst` | 1 | 0,35 s | Revienta (1 de daño en radio 1,6) | Reventón húmedo y grave con salpicadura de lodo |
| `SFX_Enemy_Toad_Hurt` | 3 | 0,1 s | Recibe un golpe | Golpe en carne blanda y un «croc» corto |
| `SFX_Enemy_Toad_Death` | 1 | 0,6 s | Muere sin reventar | Croar que se rompe y cae al lodo |

### Trampas del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Trap_Carnivore_Snap` | 2 | 0,2 s | La planta carnívora cierra la boca sobre algo (`CarnivorousPlant`) | Mandíbulas vegetales que se cierran de golpe, húmedas |
| `SFX_Trap_Carnivore_Chew` | 1 | 1,0 s | Mastica lo que atrapó (1 s) | Masticar carnoso y apretado |
| `SFX_Trap_Carnivore_Release` | 1 | 0,3 s | Suelta al jugador (medio corazón) | Escupe con un «puaj» húmedo |
| `SFX_Trap_Carnivore_Eat` | 1 | 0,7 s | Se traga a un enemigo | Trago grave y largo y un eructo vegetal |
| `SFX_Trap_Carnivore_Open` | 1 | 0,3 s | Vuelve a abrirse tras descansar | Hojas carnosas que se abren con un chasquido pegajoso |
| `SFX_Trap_Carnivore_Wither` | 1 | 0,8 s | Se seca al limpiar la sala | Planta que se arruga y cae, suspiro seco |
| `SFX_Trap_Mayfly_Swarm_Loop` | 1 | 2,0 s | Bucle mientras el enjambre de efímeras persigue al jugador | Nube de alitas finas, agudas y desordenadas |
| `SFX_Trap_Mayfly_Sting` | 3 | 0,1 s | El enjambre pica (medio corazón por segundo) | Picotazos rápidos en grupo |
| `SFX_Trap_Mayfly_Burn` | 1 | 1,2 s | El enjambre arde y cae como ascuas (no vuelve) | Zumbido que chisporrotea, se apaga y lluvia de ascuas |

### Salas del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Room_Roots_Close` | 1 | 0,6 s | Las puertas de raíces se cierran al empezar un combate (sustituye a `Room_DoorsClose` / `Room_Slabs_Close` en el piso 3) | Raíces que se retuercen y se traban de golpe, crujido de madera viva. Anuncia el combate |
| `SFX_Room_Roots_Open` | 1 | 0,7 s | Se abren al limpiar la sala (sustituye a `Room_DoorsOpen` / `Room_Slabs_Open`) | Raíces que se destraban y se retiran rozando la tierra, con un respiro |
| `SFX_Room_Cleared_Floor3` | 1 | 0,8 s | Sala limpia en el piso 3 (sustituye a `Room_Cleared`) | Mini jingle de 3 notas en re dórico (re, fa, si), con marimba de madera y un croar suave al final |
| `SFX_Room_WaveStart_Floor3` | 1 | 0,6 s | Empieza otra oleada en el piso 3 (sustituye a `Room_WaveStart`) | Llamada grave como de sapo gigante, a lo lejos |
| `SFX_Room_ConeChest_Open` | 1 | 0,8 s | Se abre un cofre de cono de Lepidodendron (`ConeChest`) | Escamas de piña leñosa que se abren una a una con crujidos y una nota cálida |
| `SFX_Room_ConeChest_Open_Big` | 1 | 1,05 s | Se abre el cono grande del Tocón hueco | Lo mismo, más grande y grave, con un brillo que sube |
| `SFX_Room_Teleport_Wake` | 1 | 0,6 s | El teletransporte de tronco hueco se activa al entrar en su sala (`Teleporter`) | Raíces que se desperezan y un zumbido que se enciende |
| `SFX_Room_Teleport_Warp` | 1 | 0,9 s | Se usa el teletransporte | Succión hacia dentro del tronco, zumbido que sube y un «plop» hueco |
| `SFX_Room_Heart_Drop` | 1 | 0,5 s | Al limpiar la sala aparece un medio corazón (`RoomHearts`) | Latido suave y un brillo que cae |
| `SFX_Room_HeartPod_Send` | 1 | 1,1 s | Con la vida llena, E envía el corazón a la vaina de la tienda (`HeartFlight`, 1,1 s) | Latido que sale volando con un silbido que se aleja |
| `SFX_Room_HeartPod_Take` | 1 | 0,5 s | E en la vaina: cura medio corazón (sustituye a `Player_Heal` en la vaina) | Vaina de semillas que se abre con un crujido y un latido cálido |
| `SFX_Room_HeartPod_Empty` | 1 | 0,25 s | E en la vaina vacía o con la vida llena (no hace nada) | Vaina que suena hueca, dos toques secos |

### Cucarachas ladronas y bichos del escenario

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Roach_Emerge` | 2 | 0,3 s | Una cucaracha sale de una grieta (`HeartThief`, `BiomassThief`) | Rasguño en la grieta y patitas que salen |
| `SFX_Roach_Scurry_Loop` | 1 | 0,5 s | Bucle mientras alguna cucaracha corre | Carrerita de patas rapidísima sobre la tierra |
| `SFX_Roach_Grab` | 2 | 0,25 s | Coge el corazón o la biomasa | Mordisco rápido y un tintineo de lo robado |
| `SFX_Roach_Hide` | 1 | 0,3 s | Se mete en otra grieta y desaparece | Rasguño que se pierde dentro |
| `SFX_Roach_Hurt` | 2 | 0,08 s | Golpeas a una ladrona de biomasa (las del corazón no se matan) | Chasquido de caparazón fino |
| `SFX_Roach_Death` | 1 | 0,4 s | Muere una ladrona y suelta lo robado | Crujido de caparazón y un chillido diminuto |
| `SFX_Critter_Scurry` | 2 | 0,25 s | Los escarabajos de decoración huyen al acercarte (`BeetleCritter`) | Patitas que se alejan, muy bajo |

### Sala secreta del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Room_RootWall_HeartPlace` | 1 | 0,8 s | Se pone el corazón morado (o la semilla) en el hueco del muro de raíces (`RootWall`; sustituye a `Room_HeartCoral`) | Latido profundo que encaja y un acorde misterioso (re frigio) |
| `SFX_Room_RootWall_Open` | 1 | 1,6 s | El muro se abre como un telón (cuadro 8 de la apertura ≈ 0,9 s) | Vetas que brillan desde el centro, raíces que crujen y se apartan, desgarro grave a los 0,9 s y un acorde morado |
| `SFX_Room_PurpleFlower_Pickup` | 1 | 0,4 s | Se coge una flor morada (`PurpleFlower`) | Pétalos que suenan como campanillas y una nota morada |
| `SFX_Room_PurpleSeed_Sprout` | 1 | 1,0 s | La semilla en el hueco brota un corazón | Brote que crece con crujidos suaves y un latido que nace |

### Salas especiales del piso 3

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Room_Log_Step` | 3 | 0,15 s | Pisar el tronco caído sobre el lodo (`FallenLog`, frecuente) | Madera hueca y mojada que cruje y se hunde un poco |
| `SFX_Room_Log_Bubble` | 2 | 0,3 s | Burbujas que suben junto al tronco | Burbuja de lodo grave |
| `SFX_Room_Coal_Ignite` | 1 | 0,8 s | La veta de carbón prende (`CoalCell`) | Llamarada honda que agarra en la roca, con un rugido que se asienta |
| `SFX_Room_Coal_Burn_Loop` | 1 | 3,0 s | Bucle mientras arde la veta (8 s y luego brasas 12 s, más bajo) | Fuego de carbón: rugido grave y constante, chasquidos de roca caliente |
| `SFX_Room_FireFront_Loop` | 1 | 3,0 s | Frente de fuego: bucle mientras el frente de helechos ardiendo avanza (`FireFrontController`) | Muro de fuego que ruge y crepita con fuerza, con ráfagas |
| `SFX_Room_Stump_Light_Loop` | 1 | 4,0 s | Tocón hueco: bucle bajo junto al rayo de luz (`StumpLight`) | Luz que vibra: coro lejano y motas que tintinean (re) |

### Jefe: Meganeura (Señora de la tormenta)

Libélula gigante de 10 u: alas enormes (zumbido grave y batido lento), quitina, viento; en la fase 3, tormenta
(lluvia, truenos y electricidad). Lo que lleva nota, en re (frigio en la tormenta: re con mi bemol). Jefe: subgrave con
un armónico medio, para que se oiga en un portátil.

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Meganeura_Intro` | 1 | 1,7 s | Presentación: llega volando y se posa (`IntroRoutine`, vuelo de 1,4 s) | Zumbido enorme que se acerca desde lejos, se frena con dos aletazos y se posa con un chasquido de patas |
| `SFX_Boss_Meganeura_Wings_Loop` | 1 | 1,0 s | Bucle mientras vuela alto (solo se ve su sombra) | Batido grave y lento de alas gigantes (re), con un soplo en cada aletazo |
| `SFX_Boss_Meganeura_TakeOff` | 1 | 0,8 s | Sube a lo alto (`TakeOff`, 0,8 s) | Aletazos que aceleran y un zumbido que sube y se aleja |
| `SFX_Boss_Meganeura_Mark` | 1 | 1,1 s | La marca ámbar del picado te sigue (1,1 s) | Pulso de zumbido que acecha, cada vez más rápido |
| `SFX_Boss_Meganeura_MarkLock` | 1 | 0,7 s | La marca se vuelve roja y se fija (0,7 s): justo después, el picado | Zumbido que se tensa y sube hasta el final |
| `SFX_Boss_Meganeura_Dive` | 2 | 0,4 s | Cae en picado (0,4 s) | Aire desgarrado por alas enormes, que baja de tono |
| `SFX_Boss_Meganeura_DiveImpact` | 2 | 0,5 s | Toca el suelo al final del picado | Golpe grave y polvo; alas que se sacuden |
| `SFX_Boss_Meganeura_TrunkStuck` | 1 | 1,5 s | Se clava en un tronco y queda atrapada 1,5 s | Golpe de madera, crujido y aleteo desesperado que forcejea hasta soltarse |
| `SFX_Boss_Meganeura_Gust` | 1 | 0,7 s | Aleteo fuerte que empuja gas, veneno y proyectiles (0,7 s) | Ráfaga de viento enorme con dos aletazos graves |
| `SFX_Boss_Meganeura_FallBurning` | 1 | 1,0 s | La niebla de metano prendida la alcanza: cae ardiendo | Chillido corto, llamas que rugen en las alas y caída pesada |
| `SFX_Boss_Meganeura_Hurt` | 3 | 0,1 s | Recibe daño (abajo) | Quitina grande que cruje, con cuerpo |
| `SFX_Boss_Meganeura_Cry` | 1 | 0,8 s | Cambia de fase (65 %): grito de aviso antes de poner huevos | Chirrido agudo de insecto gigante, áspero, que se quiebra |
| `SFX_Boss_Meganeura_EggLay` | 1 | 0,6 s | Pone un racimo de huevos en una charca (`LayEggs`) | Racimo gelatinoso que cae al agua con burbujas |
| `SFX_Boss_Meganeura_Egg_Hit` | 3 | 0,08 s | Golpeas un huevo (`MeganeuraEgg`) | Gelatina firme, sordo |
| `SFX_Boss_Meganeura_Egg_Break` | 2 | 0,35 s | Rompes un huevo | Gelatina que revienta con chapoteo |
| `SFX_Boss_Meganeura_Egg_Hatch` | 1 | 0,5 s | Sale una ninfa de un huevo | Membrana que se rasga y algo que sale chapoteando |
| `SFX_Boss_Meganeura_Nymph_Warn` | 1 | 0,4 s | La ninfa prepara el labio (0,4 s) | Clic de mandíbulas que se tensa |
| `SFX_Boss_Meganeura_Nymph_Lash` | 2 | 0,24 s | Lanza el labio extensible (~2,5 u) | Latigazo húmedo y rápido |
| `SFX_Boss_Meganeura_Nymph_Pull` | 1 | 0,4 s | El labio te arrastra | Tirón elástico y arrastre |
| `SFX_Boss_Meganeura_Nymph_Hurt` | 3 | 0,08 s | Golpeas una ninfa | Chasquido blando de larva |
| `SFX_Boss_Meganeura_Nymph_Death` | 2 | 0,3 s | Muere una ninfa (también electrocutada) | Larva que revienta con un chillido mínimo |
| `SFX_Boss_Meganeura_Screech` | 1 | 1,0 s | Rotos todos los huevos, baja furiosa: chillido con ondas (`Rage`) | Chillido enorme con ondas que se expanden (temblor grave), áspero |
| `SFX_Boss_Meganeura_Land` | 1 | 0,4 s | Se posa en el suelo (ventana de daño) | Patas que golpean el suelo y alas que se pliegan |
| `SFX_Boss_Meganeura_StormEntry` | 1 | 2,8 s | Escena de entrada de la tormenta (`StormEntry`, empieza con el chillido): oscurece y sopla la ceniza; a los 2,4 s empieza a llover. A los 2,8 s el código toca `Boss_Meganeura_Bolt_Strike` y `Boss_Meganeura_Thunder` con el primer rayo (así no pasa de 300 KB y el trueno cae justo con el dibujo) | Chillido, viento con ceniza que crece y lluvia que arranca |
| `SFX_Boss_Meganeura_Thunder` | 3 | 1,5 s | Truenos lejanos con los relámpagos que revelan su silueta | Retumbo que rueda y se aleja |
| `SFX_Boss_Meganeura_BigBolt_Warn` | 1 | 2,0 s | Señal azul del rayo grande: te sigue 1,5 s y se fija 0,5 s | Zumbido eléctrico que crece y chisporrotea, el más alto justo al caer |
| `SFX_Boss_Meganeura_LooseBolt_Warn` | 1 | 1,0 s | Marcas de los rayos sueltos en el suelo (1 s) | Crepitar eléctrico fino que sube |
| `SFX_Boss_Meganeura_Bolt_Strike` | 3 | 0,8 s | Cae un rayo (grande o suelto) | Latigazo eléctrico seco, chasquido brillante y retumbo corto |
| `SFX_Boss_Meganeura_Electrocuted` | 1 | 1,0 s | El rayo grande la alcanza: se electrocuta y cae boca arriba (0,8 s) | Descarga que zumbea, chillido que se corta y golpe al caer |
| `SFX_Boss_Meganeura_Puddle_Electric_Loop` | 1 | 1,0 s | Bucle mientras haya charcas electrificadas (5,5 s) | Zumbido eléctrico con chispas sobre agua |
| `SFX_Boss_Meganeura_Ring_Loop` | 1 | 1,0 s | Bucle de la corriente circular (4 s) | Corriente que gira: zumbido que da vueltas |
| `SFX_Boss_Meganeura_Sigillaria_Burn` | 1 | 0,9 s | Una Sigillaria quemada hace de pararrayos y se consume | Tronco que estalla en chispas y arde |
| `SFX_Boss_Meganeura_Death` | 1 | 2,8 s | Muerte (`DeathRoutine`, 2,2 s): sube, se le desprende un ala, cae en espiral a la charca | Chillido final, ala que se rompe con un crujido, zumbido roto que gira cayendo y chapuzón enorme |
| `SFX_Boss_Meganeura_StormFade` | 1 | 3,0 s | Tras su muerte, la tormenta amaina (3 s) | Lluvia que se apaga, un trueno muy lejano y gotas que caen |

### Ambiente de la tormenta

| Archivo | Uso | Duración | Notas |
|---------|-----|----------|-------|
| `SFX_Amb_Floor3_Rain_Loop.wav` | Bucle de lluvia durante la tormenta de la Meganeura (fase 3) | 8 s | Lluvia densa sobre hojas, charcas y tierra quemada, viento a ráfagas; sin ritmo |

### Jefe: Arthropleura (Coloso del Carbonífero)

Milpiés gigante de 28 segmentos: cientos de patas que repiquetean, caparazón de quitina gruesa que suena a madera
y piedra, peso enorme, carne rosada bajo las placas. No tiene voz: sus «gritos» son siseos y traqueteos de
caparazón. Jefe: subgrave con armónico medio.

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Arthropleura_Intro` | 1 | 1,5 s | Presentación: el bulto pasa tras las frondas, la tierra tiembla y asoma la cabeza (temblor de 1,5 s) | Retumbo que crece, frondas aplastadas y un traqueteo de cientos de patas que se acerca y se detiene con un siseo |
| `SFX_Boss_Arthropleura_Legs_Loop` | 1 | 1,0 s | Bucle mientras se mueve por la sala (fases 2 y 3) | Cientos de patas que repiquetean en olas sobre la tierra, con el roce de los segmentos |
| `SFX_Boss_Arthropleura_Rustle_Loop` | 1 | 2,0 s | Fase 1: bucle mientras rodea el claro por fuera, oculto tras la espesura | Frondas que se aplastan y se agitan, patas amortiguadas y un retumbo grave |
| `SFX_Boss_Arthropleura_Charge_Warn` | 1 | 1,4 s | La cabeza asoma por un hueco y apunta (1,4 s; se fija 0,35 s antes) | Traqueteo de mandíbulas que crece y un siseo que se tensa al final |
| `SFX_Boss_Arthropleura_Charge` | 2 | 0,6 s | Embiste a 20 u/s | Arranque brutal: estruendo de patas y soplo de una masa enorme |
| `SFX_Boss_Arthropleura_EdgeBreak` | 1 | 0,8 s | Revienta el borde del claro y deja una brecha | Troncos y frondas que se parten de golpe, tierra que vuela |
| `SFX_Boss_Arthropleura_Shell_Hit` | 3 | 0,1 s | Un golpe choca contra el caparazón (siempre invulnerable) | Toque sordo de quitina gruesa: dice «no hace daño» |
| `SFX_Boss_Arthropleura_Plate_Open` | 1 | 0,4 s | El fuego (o la fase 3) abre las placas de un tramo (0,36 s) | Placas que se levantan crujiendo y carne húmeda que late debajo |
| `SFX_Boss_Arthropleura_Plate_Close` | 1 | 0,4 s | Las placas se vuelven a cerrar | Placas que caen y encajan con un golpe seco |
| `SFX_Boss_Arthropleura_Hurt` | 3 | 0,1 s | Recibe daño en la carne de una placa abierta | Golpe carnoso y húmedo, con cuerpo |
| `SFX_Boss_Arthropleura_Ignite` | 1 | 0,6 s | Prende un segmento (odre, helecho o mecha) | Llamarada sobre quitina y un siseo de dolor del caparazón |
| `SFX_Boss_Arthropleura_Fuse_Loop` | 1 | 1,0 s | Bucle mientras el fuego corre por el cuerpo como una mecha (0,4 s por pieza) | Crepitar que avanza a tirones, con chasquidos de quitina caliente |
| `SFX_Boss_Arthropleura_Hiss` | 2 | 0,8 s | Dolor o furia: al arder, al quedar aturdido o al cambiar de fase | Siseo enorme de aire por los espiráculos y traqueteo de caparazón |
| `SFX_Boss_Arthropleura_WallCollapse` | 1 | 3,2 s | Al 70 % derriba la pared y la sala se triplica (escena de 3,2 s; derrumbe de 0,8 a 2,2 s) | Siseo, empujón grave, árboles y tierra que caen en cadena y polvo que se asienta |
| `SFX_Boss_Arthropleura_Stomp_Warn` | 1 | 0,9 s | Se alza para el pisotón (0,9 s) | Cuerpo que se levanta con crujidos y un zumbido grave que sube |
| `SFX_Boss_Arthropleura_Stomp_Wave` | 3 | 0,5 s | Cada una de las 3 ondas de pisotón (cada 0,3 s) | Golpe enorme contra el suelo y la onda que se aleja |
| `SFX_Boss_Arthropleura_TreeFall` | 2 | 1,2 s | Derriba un árbol (el tronco queda de cobertura) | Tronco que cruje, se parte y cae con estruendo |
| `SFX_Boss_Arthropleura_Stunned` | 1 | 1,0 s | Embiste un árbol, lo tumba y queda aturdido (3 s) | Choque de cabeza contra madera y patas que se agitan desordenadas |
| `SFX_Boss_Arthropleura_EatFern` | 2 | 0,6 s | Se come un helecho | Mandíbulas que trituran frondas, crujiente |
| `SFX_Boss_Arthropleura_Douse` | 1 | 1,0 s | Ardiendo, se mete en una charca de lodo y se apaga | Zambullida pesada, siseo de vapor enorme y burbujeo |
| `SFX_Boss_Arthropleura_Ring_Close` | 1 | 2,2 s | Fase 3: el cerco en anillo se cierra (2,2 s) | Traqueteo de patas que se acelera y aprieta, crujido de segmentos que suben de tono |
| `SFX_Boss_Arthropleura_Squeeze` | 1 | 0,6 s | Te pilla dentro y te aprieta (daño) | Presión de caparazón que cruje y un golpe sordo |
| `SFX_Boss_Arthropleura_Expel` | 1 | 0,4 s | Te expulsa del anillo | Empujón elástico y un soplo |
| `SFX_Boss_Arthropleura_Coil` | 1 | 1,0 s | Último 10 %: se enrosca en rueda | Segmentos que se pliegan uno tras otro con un clac creciente |
| `SFX_Boss_Arthropleura_Roll_Loop` | 1 | 1,0 s | Bucle mientras rueda (~8 u) | Rueda de quitina que retumba y repiquetea sobre la tierra |
| `SFX_Boss_Arthropleura_Roll_Crash` | 2 | 0,5 s | Choca contra una pared, rebota y se aturde | Estruendo de caparazón contra roca y piezas que tiemblan |
| `SFX_Boss_Arthropleura_Death` | 1 | 3,2 s | Muerte: los segmentos caen uno tras otro (desde 0,3 s, cada 0,09 s), se enrosca en espiral y queda gris y humeante | Siseo final, una cadena de crujidos de quitina que recorre el cuerpo y un golpe grave al acabar; humo que sisea |
| `SFX_Boss_Arthropleura_Smolder_Loop` | 1 | 4,0 s | Bucle muy bajo junto al cadáver humeante (escenario) | Brasas que crepitan bajo el caparazón y humo que sisea |

### Minijefe: Megarachne (el Nido)

Euriptérido que se creía araña: patas de quitina rápidas, seda que se tensa y vibra como cuerdas, ocho ojos rojos que
destellan, chillidos ásperos. Los cables de seda suenan como cuerdas graves pulsadas (re y mi bemol: inquietante).
Minijefe: algo menos grave y pesado que los jefes.

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Megarachne_Intro` | 1 | 2,0 s | Entrada: sale del embudo, se encienden los 8 ojos rojos, baja colgada y alza las patas | Seda que cruje en el embudo, ocho destellos de cristal (re y mi bemol), hilo que se desenrolla y un siseo amenazante con patas que se alzan |
| `SFX_Boss_Megarachne_CableRun_Loop` | 1 | 0,6 s | Bucle mientras corre por los cables (invulnerable) | Patas rápidas sobre seda tensa: tamborileo con cuerdas que vibran |
| `SFX_Boss_Megarachne_Walk_Loop` | 1 | 0,6 s | Bucle mientras anda por el suelo | Patas de quitina rápidas sobre tierra, algo pesadas |
| `SFX_Boss_Megarachne_Climb` | 1 | 0,4 s | Sube del suelo al cable (0,4 s) | Hilo que se recoge y patas que trepan |
| `SFX_Boss_Megarachne_Drop` | 1 | 0,48 s | Baja del cable al suelo (0,48 s) | Hilo que se suelta, caída y patas que tocan tierra |
| `SFX_Boss_Megarachne_Jump_Warn` | 1 | 1,0 s | Marca el salto desde el cable (1 s; se fija 0,3 s antes) | Cable que se tensa y cruje cada vez más, un siseo al final |
| `SFX_Boss_Megarachne_Jump_Land` | 2 | 0,4 s | Cae del salto (ventana de daño de 1,5 s) | Golpe de cuerpo y ocho patas que se clavan |
| `SFX_Boss_Megarachne_SilkVolley` | 1 | 0,5 s | Ráfaga de 5-7 bolitas de seda en abanico (salen a los 0,36 s) | Abdomen que se contrae y varios «pfft» de seda casi juntos |
| `SFX_Boss_Megarachne_Silk_Splat` | 3 | 0,12 s | Cada bolita cae y deja una telaraña pequeña | Pegote de seda pegajosa |
| `SFX_Boss_Megarachne_Net_Warn` | 1 | 1,2 s | Marca dónde caerá la red (1,2 s; se fija 0,4 s antes) | Hilos que se tejen arriba, cada vez más tensos |
| `SFX_Boss_Megarachne_Net_Drop` | 1 | 0,5 s | La red cae (0,4 s) | Red que silba al caer y golpea el suelo con muchos hilos |
| `SFX_Boss_Megarachne_Net_Trap` | 1 | 0,4 s | La red atrapa al jugador (1 s) | Hilos que se cierran y tiran, elásticos |
| `SFX_Boss_Megarachne_Sac_Weave` | 1 | 0,9 s | Teje el saco de crías bajo un cable (0,88 s) | Hilera que gira y teje, seda que se enrolla |
| `SFX_Boss_Megarachne_Sac_Tremble` | 1 | 1,0 s | El saco tiembla 1 s antes de reventar | Saco que palpita y cosas que se mueven dentro, cada vez más |
| `SFX_Boss_Megarachne_Sac_Hit` | 3 | 0,08 s | Golpeas el saco | Seda gruesa y blanda |
| `SFX_Boss_Megarachne_Sac_Break` | 1 | 0,5 s | Rompes el saco antes de que reviente (no salen crías) | Seda que se rasga y algo que se apaga con un chillido diminuto |
| `SFX_Boss_Megarachne_Sac_Burst` | 1 | 0,6 s | El saco revienta y salen 3-4 crías | Reventón de seda y crías que salen correteando |
| `SFX_Boss_Megarachne_Brood_Loop` | 1 | 0,5 s | Bucle mientras haya crías vivas | Correteo fino de patitas, nervioso |
| `SFX_Boss_Megarachne_Brood_Bite` | 3 | 0,1 s | Una cría muerde | Mordisquito seco |
| `SFX_Boss_Megarachne_Brood_Death` | 3 | 0,2 s | Muere una cría | Chasquido blando y chillido mínimo |
| `SFX_Boss_Megarachne_Cable_Weave` | 1 | 3,0 s | Teje un cable nuevo (3 s): un hilo fino cruza el suelo, vulnerable | Hilo que se estira y se tensa poco a poco, tañidos que suben, patas que trabajan |
| `SFX_Boss_Megarachne_Cable_Ignite` | 1 | 0,5 s | Un cable prende | Seda que se enciende con un chisporroteo rápido |
| `SFX_Boss_Megarachne_Cable_Fuse_Loop` | 1 | 1,0 s | Bucle mientras el fuego corre por un cable como una mecha (6 u/s) | Chisporroteo que corre y cuerda que zumba al quemarse |
| `SFX_Boss_Megarachne_Cable_Snap` | 1 | 0,45 s | El cable quemado se rompe | Cuerda que revienta con un tañido roto y un latigazo |
| `SFX_Boss_Megarachne_Fall_Stunned` | 1 | 1,0 s | El fuego la alcanza en el cable: cae y queda aturdida (3 s) | Chillido corto, caída pesada y patas que se agitan |
| `SFX_Boss_Megarachne_Hurt` | 3 | 0,1 s | Recibe daño en el suelo | Quitina que cruje y un chillido seco |
| `SFX_Boss_Megarachne_Rage_Screech` | 1 | 2,5 s | Al 50 %: vuelve al Nido y chilla (2,5 s) | Chillido áspero que crece, cables que vibran todos a la vez y el árbol que cruje |
| `SFX_Boss_Megarachne_Nest_Wither` | 1 | 1,3 s | El árbol del Nido se seca | Madera que cruje y se agrieta, hojas que se secan y un suspiro que baja |
| `SFX_Boss_Megarachne_Nest_Burn` | 1 | 1,5 s | El Nido arde una sola vez | Llamarada que sube por la copa y seda que chisporrotea |
| `SFX_Boss_Megarachne_Web_Collapse` | 1 | 1,0 s | La red de la copa se cae | Hilos que se rompen uno tras otro y una red que se desploma |
| `SFX_Boss_Megarachne_Death` | 1 | 1,35 s | Muerte: cae panza arriba y encoge las patas | Chillido que se rompe, golpe de espaldas y patas que se encogen crujiendo |
| `SFX_Boss_Megarachne_Cables_Drop` | 1 | 1,2 s | Los cables se descuelgan al morir | Cuerdas que se aflojan con tañidos que bajan y caen al suelo |
| `SFX_Boss_Megarachne_Cocoon_Fall` | 1 | 0,9 s | Cae el capullo de seda con la recompensa (0,9 s) | Hilo que se suelta, capullo que baja y un golpe blando |
| `SFX_Boss_Megarachne_Cocoon_Open` | 1 | 0,8 s | El capullo se abre y aparece el contenedor de corazón | Seda que se rasga y un latido con un brillo |

### Jefe secreto: Lepidodendron ancestral (Corazón del bosque)

Un árbol inmenso y antiguo: madera que gime y cruje, raíces que se mueven bajo tierra, conos pesados, esporas, y la
luz dorada del final. Lo que lleva nota, en re frigio (re y mi bemol), como el resto de lo secreto y morado; la
recompensa se resuelve en re mayor (fa sostenido). Jefe: subgrave con armónico medio.

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Lepidodendron_Intro` | 1 | 3,0 s | Presentación: la cámara recorre el árbol (1,5 s) y se queda en él | Gemido de madera enorme que despierta, hojas de la copa que se agitan y un acorde grave en re frigio |
| `SFX_Boss_Lepidodendron_Creak_Loop` | 1 | 4,0 s | Bucle bajo en la arena del árbol mientras vive (la madera respira) | Crujidos lentos de tronco, hojas en lo alto y un fondo grave que respira |
| `SFX_Boss_Lepidodendron_RootWall_Hit` | 3 | 0,1 s | Golpe a una pared de raíces | Madera viva y dura, golpe seco |
| `SFX_Boss_Lepidodendron_RootWall_KnotHit` | 3 | 0,12 s | Golpe a un nudo débil (daño ×2) | Golpe más hueco y jugoso, con un crujido que se abre |
| `SFX_Boss_Lepidodendron_RootWall_Ignite` | 1 | 0,6 s | La pared prende (odres, helechos o el cono del bombardero) | Llamarada que agarra en la madera, con un gemido de la raíz |
| `SFX_Boss_Lepidodendron_RootWall_Burn_Loop` | 1 | 2,0 s | Bucle mientras la pared arde (el fuego se extiende en 3 s) | Madera gruesa que arde: rugido grave y chasquidos de savia |
| `SFX_Boss_Lepidodendron_RootWall_Retract` | 1 | 1,2 s | La pared llega a cero y se retira bajo tierra | Raíces que se arrastran hacia abajo, tierra que se remueve y un gemido que se hunde |
| `SFX_Boss_Lepidodendron_RootWall_Crumble` | 1 | 1,2 s | Si estaba totalmente carbonizada, se desmorona en ceniza | Carbón que se raja y se deshace en ceniza con un suspiro |
| `SFX_Boss_Lepidodendron_Wave_Spawn` | 2 | 0,5 s | Salen enemigos de la oleada (destello de 0,25 s) | Tierra que se abre de golpe con un brote de esporas |
| `SFX_Boss_Lepidodendron_SporeArc` | 1 | 0,6 s | Lanza esporas en arco por encima de las paredes | Soplo grave del árbol y esporas que silban hacia arriba |
| `SFX_Boss_Lepidodendron_SporeRain_Warn` | 1 | 1,2 s | Lluvia de esporas: marcas rojas en el suelo (caen a los 1,2 s) | Siseo de esporas cayendo que crece hasta el impacto |
| `SFX_Boss_Lepidodendron_Spore_Impact` | 3 | 0,25 s | Cada espora cae | «Puf» seco de esporas contra el suelo |
| `SFX_Boss_Lepidodendron_Phase_Final` | 1 | 1,5 s | Llega al centro (fase final, 25 %): el árbol se despierta del todo | Gemido profundo, la copa entera que se sacude y conos que crujen arriba |
| `SFX_Boss_Lepidodendron_Trunk_Hit` | 3 | 0,1 s | Golpe al tronco (−80 % de daño) | Golpe muy sordo en madera maciza: dice «casi no hace daño» |
| `SFX_Boss_Lepidodendron_Cone_Warn` | 1 | 1,5 s | Un cono se prepara para bajar (1,5 s; marca que te sigue) | Rama que cruje y se tensa arriba, cada vez más |
| `SFX_Boss_Lepidodendron_Cone_Descend` | 1 | 1,1 s | La sombra se fija (0,72 s) y el cono baja colgado (0,35 s) | Rama que se dobla con un gemido y un silbido que baja |
| `SFX_Boss_Lepidodendron_Cone_Slam` | 2 | 0,6 s | El cono golpea el suelo (radio 1,6) | Golpe enorme de piña leñosa contra la tierra, con astillas |
| `SFX_Boss_Lepidodendron_Cone_Lift` | 1 | 1,0 s | El cono vuelve a subir (1 s) | Rama que tira hacia arriba crujiendo |
| `SFX_Boss_Lepidodendron_Armor_Hit` | 3 | 0,1 s | Golpe a la armadura de ramas del cono | Ramas entrelazadas que crujen |
| `SFX_Boss_Lepidodendron_Armor_Break` | 1 | 0,7 s | La armadura se rompe (golpes o fuego) y deja el cono expuesto | Ramas que se parten en cadena y se abren |
| `SFX_Boss_Lepidodendron_Cone_Hurt` | 3 | 0,1 s | Daño al cono expuesto (daño completo) | Golpe carnoso de cono verde, con savia |
| `SFX_Boss_Lepidodendron_Cone_Break` | 1 | 0,9 s | El cono se rompe y deja de atacar | Piña que revienta en escamas y esporas, con un gemido del árbol |
| `SFX_Boss_Lepidodendron_RootLash_Warn` | 1 | 0,8 s | Latigazo de raíces: aviso en el suelo (0,8 s) | Tierra que tiembla y se agrieta en línea, cada vez más |
| `SFX_Boss_Lepidodendron_RootLash` | 2 | 0,5 s | La raíz sale y azota | Raíz que revienta la tierra y restalla |
| `SFX_Boss_Lepidodendron_SpikeRing_Warn` | 1 | 0,78 s | Corro de púas: aviso (0,78 s) | Crujido en anillo bajo tierra que sube |
| `SFX_Boss_Lepidodendron_SpikeRing` | 1 | 0,35 s | Salen las púas en corro | Muchas púas de madera que brotan a la vez |
| `SFX_Boss_Lepidodendron_Corner_Warn` | 1 | 1,0 s | Castigo en las esquinas: aviso en dos esquinas opuestas (1 s) | Raíces que se agitan en las esquinas, un retumbo que crece |
| `SFX_Boss_Lepidodendron_Corner_Strike` | 1 | 0,6 s | Las raíces golpean las esquinas | Golpe doble de raíces gruesas y tierra que vuela |
| `SFX_Boss_Lepidodendron_Methane_Warn` | 1 | 1,0 s | Nubes de metano junto al tronco (1 s de aviso) | Burbujeo de gas que sale de las raíces |
| `SFX_Boss_Lepidodendron_Tree_Shake` | 1 | 1,0 s | El metano prendido junto al tronco sacude el árbol y baja los conos | Golpe en el tronco, la copa entera que se sacude y un gemido |
| `SFX_Boss_Lepidodendron_Death_Crack` | 1 | 1,6 s | Desplome, parte 1: las vetas brillan (0,7 s) y el tronco se agrieta (0,9 s) | Gemido que crece, luz que zumba y grietas que corren por la madera |
| `SFX_Boss_Lepidodendron_Death_Fall` | 1 | 2,4 s | Desplome, parte 2: el tronco cae hacia la entrada; golpe a los 1,77 s | Madera que se rasga, caída que acelera con un silbido enorme, impacto brutal y polvo que se asienta |
| `SFX_Boss_Lepidodendron_LightColumn` | 1 | 2,5 s | La copa rasgada deja entrar la columna de luz dorada | Coro luminoso que se abre en re mayor, campanas suaves y motas que tintinean |
| `SFX_Boss_Lepidodendron_Reward_Descend` | 1 | 2,2 s | El corazón y la Espora morada bajan por la luz (empieza a los 0,4 s, baja en 1,8 s) | Campanas que descienden despacio y un latido al llegar |
| `SFX_Boss_Lepidodendron_Heart_Pickup` | 1 | 0,8 s | Se coge el corazón permanente (+1) | Latido doble y un acorde dorado |
| `SFX_Boss_Lepidodendron_PurpleSpore_Pickup` | 1 | 1,0 s | Se coge la Espora morada (llave del piso 4) | Acorde morado (re frigio) que brilla y se queda vibrando |
| `SFX_Boss_Lepidodendron_Stump_Enter` | 1 | 0,8 s | E en la grieta de luz del tocón: entras en su sala | Madera que se abre y un soplo cálido de luz |
| `SFX_Boss_Lepidodendron_Relic_Pickup` | 1 | 1,5 s | Se coge la reliquia Raíz ancestral del pedestal (presentación con halo) | Raíces que vibran, un acorde mayor amplio y un brillo que se expande |

## Música (tanda 10)

Decidido con la usuaria el 2026-11-25: una música por tramo del incendio, con **dos capas sincronizadas**
(exploración y combate) como en los pisos 1 y 2, y fundido cruzado de unos 3 s al cambiar de tramo. Todo en re: cada
tramo cambia el modo. Pauta de tamaño subida a ~16-18 MB para este piso (la música nueva ocupa ~11,6 MB). Generada
por `musica_piso3.py`.

| Archivo | Uso | Duración | Ambiente |
|---------|-----|----------|----------|
| `Music_Floor3_Green_Explore.ogg` | Pantano verde (primer tercio), sin combate (bucle, capa A) | 24 compases a 98 BPM (58,8 s) | Re dórico, húmedo y vivo: marimba de madera en arpegios, flauta de caña con eco, pad que ondula, sub, croar como bombo y semillas |
| `Music_Floor3_Green_Combat.ogg` | Pantano verde con las puertas cerradas (bucle, capa B) | Igual que la A | La misma armonía con tambores de tronco sincopados, bajo pulsado y la marimba doblando la melodía |
| `Music_Floor3_Smoke_Explore.ogg` | Bosque humeante (segundo tercio), sin combate (bucle, capa A) | 24 compases a 105 BPM (54,9 s) | Re eólico, tenso y velado: pad oscuro que ondula despacio, ostinato de cuerda grave, la caña una octava abajo, chasquidos de brasa |
| `Music_Floor3_Smoke_Combat.ogg` | Bosque humeante con las puertas cerradas (bucle, capa B) | Igual que la A | Tambores de tronco insistentes, bajo de sierra, ostinato en semicorcheas, lead con eco |
| `Music_Floor3_Burnt_Explore.ogg` | Tierra quemada (último tercio), sin combate (bucle, capa A) | 24 compases a 112 BPM (51,4 s) | Re frigio dominante (re, mi bemol, fa sostenido), seco y ardiente: metales graves que se hinchan, arpa con el semitono, percusión de brasas |
| `Music_Floor3_Burnt_Combat.ogg` | Tierra quemada con las puertas cerradas (bucle, capa B) | Igual que la A | Tambor hondo, caja grande, brasas en semicorcheas, golpes de metal y bajo con el semitono |
| `Music_Boss_Meganeura.ogg` | Meganeura, fases 1 y 2 (bucle) | 24 compases a 126 BPM (45,7 s) | Re frigio: ostinato de alas (marimba en semicorcheas re–mi bemol), cuerdas que tiemblan, batería completa, lead amenazante con eco |
| `Music_Boss_Meganeura_Storm.ogg` | Meganeura, fase 3: la tormenta (bucle) | 28 compases a 140 BPM (48 s) | Caótica: bombo en cada pulso, bajo en semicorcheas, racimos de coro disonantes, truenos fuera de compás y lluvia |
| `Music_Boss_Arthropleura.ogg` | Arthropleura, fases 1 y 2 (bucle) | 16 compases a 84 BPM (45,7 s) | Re eólico, pesadísima: bombo enorme y tambor hondo, toms en olas como las patas, metales graves, quitina |
| `Music_Boss_Arthropleura_Coil.ogg` | Arthropleura, fase 3 y la rueda (bucle) | 20 compases a 105 BPM (45,7 s) | La misma idea acelerada: bombo en cada pulso, olas de toms continuas, golpes de metal, lead con eco |
| `Music_Boss_Megarachne.ogg` | Megarachne antes del enfado (bucle) | 24 compases a 120 BPM (48 s) | Re con mi bemol: ostinato de cuerdas de seda pulsadas, quitina en semicorcheas, voz fina de cristal con eco |
| `Music_Boss_Megarachne_Rage.ogg` | Megarachne enfadada, desde el 50 % (bucle) | 24 compases a 126 BPM (45,7 s) | Más rápida y densa: seda en semicorcheas, bajo de sierra, bombo en cada pulso, redobles |
| `Music_Boss_Lepidodendron.ogg` | Lepidodendron, paredes de raíces (bucle) | 16 compases a 72 BPM (53,3 s) | Re frigio, majestuoso: órgano de madera, coro, tambor hondo, el gemido del árbol y la caña con eco |
| `Music_Boss_Lepidodendron_Final.ogg` | Lepidodendron, fase final (25 %) (bucle) | 20 compases a 90 BPM (53,3 s) | Ostinato de órgano de madera, coro, tambores y caja grande, bajo con el semitono, lead con eco |
| `Music_Sting_SecretBoss.ogg` | Presentación del jefe secreto (sello «JEFE SECRETO» morado), en lugar de `Music_Sting_BossIntro` | ~4,8 s | Golpe grave, racimo frigio morado de coro y órgano, campanas de cristal |
| `Music_Sting_Relic.ogg` | Al coger una reliquia (Raíz ancestral) | ~4,3 s | Acorde amplio de re mayor, arpa que sube y un brillo dorado |

La tienda (`Music_Shop`), la sala de cambio (`Music_Exchange`), victoria, derrota, evolución de linaje y la
presentación de los jefes normales se reutilizan.

## Ambientes de los tramos y salas

| Archivo | Uso | Duración | Notas |
|---------|-----|----------|-------|
| `SFX_Amb_Floor3_Smoke_Loop.wav` | Debajo de la música en el bosque humeante, en lugar de `Amb_Floor3_Loop` (bucle, muy bajo) | 24 s | El pantano con humo: menos vida, crepitar lejano, ramas que crujen, viento cálido, un croar suelto |
| `SFX_Amb_Floor3_Burnt_Loop.wav` | Debajo de la música en la tierra quemada (bucle, muy bajo) | 24 s | Viento seco a ráfagas, brasas que crepitan y estallan, fondo en re con mi bemol; sin vida |
| `SFX_Amb_Floor3_BossRoom_Loop.wav` | Salas de los jefes del piso 3 antes y después del combate (bucle) | 20 s | Claro cerrado, grave y tenso: madera que cruje, frondas y burbujas de lodo |
| `SFX_Amb_Floor3_Amber_Loop.wav` | Sala del ámbar (bucle; igual en todos los tramos: el fuego no llega) | 20 s | Refugio cálido: zumbido dorado en re, resina que gotea, destellos de cristal y un insecto atrapado muy lejos |
