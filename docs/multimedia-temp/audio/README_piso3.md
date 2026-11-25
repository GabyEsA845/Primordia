# Audio del piso 3: cómo conectarlo en el juego

**ESTADO (2026-11-25): todo lo de esta guía está CONECTADO en Unity** (tandas 0-10, rama `feature/floor3-audio-impl`; ver `docs/memoria-claude/primordia-floor3-audio.md`). Quedan fuera por decisión: `Skin_Mucus_Reflect`, un `Boss_Meganeura_Thunder` por cada relámpago y el sonido de la sala de paso.

Guía para quien integre el audio del piso 3 (pensada para Sonnet). Los sonidos ya están generados y aprobados por
la usuaria en `docs/multimedia-temp/audio/` (`efectos_piso3.py`, tablas en `PROMPT_piso3.md`; muestrario solo del
piso en `muestrario_piso3.html`). **No se regeneran ni se retocan sin que ella lo pida.** Aquí va lo que no es
evidente: qué sonidos son bucles, cuáles sustituyen a otros, cuáles se repiten mucho y qué casos piden lógica.

Se completa tanda a tanda. Hecho hasta ahora: **tanda 1** (reacciones y terreno), **tanda 2** (jugador: esquivas,
Bocas y mutaciones), **tanda 3** (especiales del ámbar, clases y linajes), **tanda 4** (enemigos y trampas), **tanda 5** (salas y escenario), **tanda 6** (Meganeura), **tanda 7** (Arthropleura), **tanda 8** (Megarachne), **tanda 9** (Lepidodendron) y **tanda 10** (música y ambientes). Solo queda el sonido de la sala de paso del piso 3, que se hará con la música del piso 4.

## Cómo funciona el sistema (lo que hay que saber antes)

- **Importar:** menú **Primordia > Audio > Importar audio** (`Editor/AudioSetup.cs`) con Unity abierto, o en
  batchmode (ver `docs/memoria-claude/primordia-unity-batchmode.md`). Copia los WAV a
  `Assets/_Primordia/Audio/SFX`, rehace la `SoundLibrary` desde `sonidos.json` y rellena los campos de sonido de
  los datos (`AttackData`, `EnemyData`, `MutationData`).
- **Nombres:** el id es el nombre sin `SFX_` ni número de variante (`SFX_Fire_Ignite_02.wav` → `Fire_Ignite`).
  Cada id nuevo va como constante en `Scripts/Audio/Sounds.cs` (el lint comprueba que `Sounds.cs` y
  `sonidos.json` coincidan).
- **API** (`Scripts/Audio/AudioManager.cs`, clase `Sfx`):
  - `Sfx.Play(id, volume = 1, pitch = 1)`: un disparo; elige variante al azar y aplica `pitchJitter`.
  - `Sfx.Stop(id)`: corta ese efecto con un fundido de 40 ms (para avisos que se cancelan).
  - `Sfx.StartLoop(id)` / `Sfx.StopLoop(id)`: **una sola instancia por id**. Llamar a `StartLoop` otra vez no
    duplica el sonido (solo actualiza su volumen), pero un `StopLoop` lo apaga para todos. Por eso los bucles que
    pueden pedir varias fuentes a la vez necesitan **un contador** o un dueño único (ver abajo).
  - `AudioManager.Instance.StartLoop(id, volumeScale)` admite volumen: sirve para bajar un bucle con la distancia.
- **Sin posición:** los efectos son 2D, no se atenúan con la distancia. Si un sonido de escenario debe oírse solo
  cerca (bucles de fuego, niebla), el código tiene que decidir cuándo sonar o escalar el volumen.
- **Repeticiones:** `AudioSetup.Intervals` fija el intervalo mínimo entre dos disparos del mismo id (por defecto
  muy corto). Los frecuentes de abajo necesitan su entrada para no amontonarse cuando muchas fuentes lo piden en el
  mismo instante.
- **Avisos:** los sonidos de aviso duran exactamente lo mismo que el aviso del juego y llegan a lo más alto justo
  cuando ocurre la acción. Se lanzan **al empezar** el aviso, y si el aviso se cancela se cortan con `Sfx.Stop`.
- Probar con la escena del piso 3; **commit solo cuando la usuaria lo confirme**.

## Tanda 1: reacciones y terreno

| Id | Dónde y cómo |
|---|---|
| `Fire_Ignite` | Al nacer un `GroundFire` (y al prender charco, telaraña o maleza si no tienen sonido propio). Muchos fuegos nacen a la vez al propagarse: intervalo ~0,12 s |
| `Fire_Burn_Loop` | **Bucle único de escenario.** Encenderlo mientras exista algún `GroundFire` a menos de ~8 u del jugador y apagarlo cuando no quede ninguno. Recomendado: un gestor estático (contador de fuegos activos cerca o un sondeo cada 0,25 s) en lugar de que cada fuego llame a Start/Stop. Bajar el volumen con la distancia al más cercano |
| `Fire_Extinguish` | Cuando un fuego se apaga por agua, lodo o lluvia (no cuando se consume solo). `PlayerMudSink` apaga al jugador: aquí suena. La lluvia de la Meganeura puede apagar muchos: intervalo ~0,2 s |
| `Status_Burn_Start` / `Status_Burn_Tick` | Inicio de quemadura (`Burn`, `PlayerBurn`) y cada daño de fuego. El tic es frecuente: intervalo ~0,1 s |
| `Methane_Explosion` | En `Explosion` (todas las explosiones de gas: odre, nube del Saco de metano, burbuja). Varias en cadena: intervalo ~0,08 s |
| `Methane_GasBag_Burst` | Justo antes de la explosión del odre (`MethaneSac`); suenan los dos juntos. `Methane_GasBag_Regrow` al volver a crecer |
| `Methane_FogFlash` | Al prender la niebla (`MethaneMist`), una vez por niebla, no por casilla que se propaga |
| `Methane_Mist_Loop` | Bucle muy bajo mientras el jugador esté sobre la niebla o a ~3 u; mismo patrón de gestor que el fuego |
| `Methane_Bubble_Warn` / `Methane_Bubble_Pop` | `MethaneBubbler` (sala Burbujeo): el aviso dura 1 s, igual que `WarnSeconds`. Si hay varias burbujas a la vez, intervalo ~0,3 s en el aviso |
| `Poison_Puddle` | Al nacer un `PoisonPuddle` (vainas de la Medullosa, Escupitajo, muerte del envenenado con Toxina). Intervalo ~0,1 s |
| `Poison_ToxicCloud` | Al nacer una `ToxicCloud` (fuego + veneno) |
| `Poison_Bulb_Burst` / `Poison_Bulb_Regrow` | `PoisonBulb`: al reventar y al volver a crecer (25 s) |
| `Fumarole_Warn` / `Fumarole_Erupt` | `SteamVent` (es la fumarola): el aviso dura 1 s (`WarnSeconds`). Sala Fumarolas: varias en patrón, intervalo ~0,25 s en ambos |
| `Fern_Rustle` | Al cruzar un helecho (`Fern`, animación de roce). Frecuente: intervalo ~0,15 s |
| `Fern_Burn` | Cuando un helecho empieza a arder (no en cada fotograma de la quema) |
| `Brush_BurnAway` | Cuando la maleza (`BrushWall`) se deshace y abre el paso |
| `Mud_Sink_Loop` | **Bucle del jugador:** `StartLoop` al empezar a hundirse en `PlayerMudSink` y `StopLoop` al salir o al hundirse del todo |
| `Mud_Escape` | El jugador sale del lodo esquivando |
| `Mud_Drown` | Hundido del todo (−½ corazón y vuelve a tierra) |
| `Mud_Splash` / `Water_Splash` | Pisar o caer en lodo o agua. **También al aterrizar las Patas de salto sobre lodo o agua**, junto con `Limbs_Jump_Land`. Intervalo ~0,1 s |
| `Web_Stick` / `Web_Burn` | `SpiderWeb`: al quedarse pegado (jugador o enemigo) y al arder |

## Tanda 2: jugador

| Id | Dónde y cómo |
|---|---|
| `Player_Dodge_Frog`, `Player_Dodge_Diplocaulus`, `Player_Dodge_Scorpion` | **Sustituyen a `Player_Dodge`** en los cuerpos del piso 3 (rana = Área, *Diplocaulus* = cuerpo a cuerpo, vinagrillo = proyectil). Elegir por la familia del cuerpo actual en el sitio donde hoy suena `Player_Dodge`. Con Patas de salto equipadas la esquiva es un salto: suena `Limbs_Jump_Takeoff` en lugar de estos |
| `Mouth_Spit_Shoot` | Sale la bola del Escupitajo. Probablemente se asigna en `AudioSetup` al `AttackData` de la Boca (como las Bocas del piso 2) |
| `Mouth_Spit_Trail` | Cada charquito del rastro (cada 0,5 s). Muy bajo, intervalo ~0,2 s (con Camada hay 4 bolas) |
| `Mouth_Spit_Burst` | `PoisonSpitBurst` al reventar. `Mouth_Spit_Split` al dividirse (Camada, nivel III) |
| `Mouth_Spin_Start` + `Mouth_Spin_Loop` | Al empezar el giro: `Play(Start)` y `StartLoop(Loop)`; `StopLoop` al soltar o al llegar al máximo. Después, `Mouth_Spin_Dizzy` si hay mareo |
| `Mouth_Spin_Hit` | Cada golpe del giro (cada 0,25 s). Si alcanza a varios enemigos en el mismo tic, **un solo sonido** (intervalo ~0,1 s) |
| `Mouth_Spin_Deflect` | El giro devuelve un proyectil (nivel II) |
| `Mouth_Spin_Boomerang` | Al lanzar el bumerán (`SpinBoomerang`); el sonido ya incluye la ida y la vuelta (0,6 s). Si el viaje real dura mucho más, se puede alargar en el generador |
| `Mouth_Vinegar_Start` + `Mouth_Vinegar_Loop` | `JetAttack`: `Play(Start)` y `StartLoop(Loop)` al empezar a mantener; `StopLoop` al soltar o al vaciarse |
| `Mouth_Vinegar_Hit` | Cada impacto (cada 0,15 s), **uno por tic**, aunque toque a varios. Intervalo ~0,1 s |
| `Mouth_Vinegar_Empty` / `Mouth_Vinegar_Ready` | La barra se vacía / vuelve a estar llena (no suena Ready si nunca se vació del todo ni al empezar la sala) |
| `Mouth_Vinegar_Splash` | Cada estallido de Salpicadura (nivel III, cada 0,5 s) |
| `Limbs_Jump_Takeoff` / `Limbs_Jump_Land` / `Limbs_Jump_Stun` | `JumpingLegs`: despegue, aterrizaje (más `Mud_Splash` o `Water_Splash` según el suelo) y aturdimiento de Rana toro (nivel III, en lugar de nada extra; se puede sumar a Land) |
| `Limbs_Spinner_Thread` | Al tender cada hilo (`SilkThreads`). `Limbs_Spinner_Snare` cuando un enemigo toca un hilo (intervalo ~0,2 s). `Limbs_Spinner_Web` al formarse la telaraña del cruce (`SilkWeb`) |
| `Skin_Mucus_Charged` / `Skin_Mucus_Absorb` / `Skin_Mucus_Reflect` | `AmphibianMucus`: moco cargado, golpe absorbido y proyectil devuelto (nivel III; en ese caso suena Reflect en vez de Absorb) |
| `Skin_Char_Kindle` | `CharredScales`: al activarse el «tus ataques queman 4 s» tras pisar fuego. No repetirlo mientras se renueva pisando fuego (intervalo ~3 s) |
| `Skin_Char_Flare` | Brasa (nivel III): la llamarada al recibir un golpe |
| `Organ_Methane_Release` + `Organ_Methane_Cloud_Loop` | Al soltar la nube (`MethaneCloud`): `Play(Release)` y `StartLoop(Cloud_Loop)`. Con nivel II hay 2 nubes: **contador**; `StopLoop` cuando no quede ninguna (la explosión ya suena con `Methane_Explosion`) |
| `Organ_Pheromone_Cast` | Al lanzar las Feromonas. `Status_Confused` en cada enemigo que queda confundido (`Confusion`), intervalo ~0,15 s |

## Tanda 3: especiales del ámbar, clases y linajes

| Id | Dónde y cómo |
|---|---|
| `Room_AmberBlock_Break` | `AmberBlock`: al romperse con E (antes de que salga la burbuja). Sustituye al sonido que suene ahora |
| `Special_Cerci_Dash` / `Special_Cerci_Ready` | `CerciDodge`: cada quiebro y cada carga que vuelve (con 2 cargas en nivel III, suena por carga) |
| `Special_Capsule_Encase` | `AmberCapsuleShield`: al encerrarte. Justo después, `Special_Capsule_Heal` con la cura |
| `Special_Capsule_Hit` | Golpes que chocan contra la cápsula mientras estás dentro (intervalo ~0,1 s) |
| `Special_Capsule_Shatter` | Al salir y estallar en esquirlas |
| `Special_Drop_Shoot` | `AmberDropShot`: sale la gota |
| `Special_Drop_Encase` | `AmberCocoon`: el enemigo queda encerrado (tras `FormDelay`, 0,12 s) |
| `Special_Drop_Hit` | Daño al enemigo encerrado o proyectil que para el bloque (intervalo ~0,1 s) |
| `Special_Drop_Shatter` | El bloque se rompe (también el del cadáver fosilizado del nivel III) |
| `Special_Drop_Fossil` | Nivel III: en lugar de `Special_Drop_Encase`, el enemigo muere y se fosiliza (dura lo mismo que `FossilSeconds`, ~0,5 s) |
| `Special_Drop_Ready` | `AmberGlandGlow`: la gota vuelve a estar lista |
| `Lineage_Parasite_Attach` | `Parasite`: se pega uno |
| `Lineage_Parasite_Suck` | Cada daño de 1 por segundo de cada parásito. Con hasta 3 (6 con la Lamprea) vivos: intervalo ~0,25 s y muy bajo |
| `Lineage_Parasite_Drain` | `ParasiteBenefit`: cada medio corazón curado |
| `Lineage_Parasite_Jump` | Salto al enemigo más cercano al morir el huésped (dura 0,35 s, como `LeapSeconds`) |
| `Lineage_Colonize_Floor3` | **Sustituye a `Lineage_Colonize`** en el piso 3: en `ColonyBenefit` (hoy llama a `Sounds.LineageColonize` en dos sitios), elegir por piso |
| `Lineage_Mushroom_Sprout` | Brota la mata grande al morir un enemigo encima |
| `Lineage_Eryops_Wade_Loop` | `EryopsTouch`: bucle mientras el jugador cruza lodo con el toque (StartLoop al entrar, StopLoop al salir; mientras se mueve, o bajar el volumen si está quieto) |
| `Lineage_Eryops_Emerge` | Al salir del lodo (empieza el impulso de 5 s) |
| `Lineage_Xenacanthus_Spine` | Al golpear a un confundido que cuenta como herido (en lugar de `Lineage_Finisher` en ese caso) |
| `Lineage_GasSting_Toxify` | Aguijón de gas: el chorro convierte metano en nube tóxica (en lugar de `Methane_Explosion`) |
| `Lineage_Centipede_Puddle` | `CentipedeTouch`: cada aterrizaje que deja charco (además de `Limbs_Jump_Land`; no suena también `Poison_Puddle`) |
| `Lineage_Tinder_Shoot` / `_Trail` / `_Burst` | Yesca: **sustituyen** a `Mouth_Spit_Shoot` / `_Trail` / `_Burst` cuando el linaje Yesquero está completo. `_Trail` es frecuente (intervalo ~0,15 s) |
| `Lineage_Zombie_Rise` / `Lineage_Zombie_Collapse` | `ZombieAlly`: al levantarse (0,5 s, como `RiseSeconds`) y al caer de verdad |
| `Lineage_Lamprey_Attach` | `LampreyTouch`: girando, cuando se pega el segundo parásito (en lugar de `Lineage_Parasite_Attach`) |
| `Lineage_Leech_Burst` | `LeechTouch`: al reventar la bola grande y saltar parásitos (en lugar de `Mouth_Spit_Burst`) |

## Tanda 4: enemigos y trampas

Golpe y muerte de cada enemigo van en `EnemyData` (`hurtSound`, `deathSound`; también hay `alertSound` y
`aimSound`): asignarlos en `AudioSetup` como los de los pisos 1 y 2. Los retoños de Calamites tienen su propio
`EnemyData` (`CalamitesSprout`). Ningún enemigo que caiga al lodo profundo y se hunda debe hacer su sonido de muerte
normal si no muere (mismo criterio que la fosa del piso 2).

| Id | Dónde y cómo |
|---|---|
| `Enemy_Calamites_Sprout` / `Enemy_Calamites_SporeRing` | `CalamitesBrain`: al echar un retoño y al lanzar el anillo de esporas |
| `Enemy_CalamitesSprout_Wither` | Al morir la madre, **una sola vez** aunque se sequen 4 retoños. Los retoños que se secan así **no** hacen `CalamitesSprout_Death` |
| `Enemy_Medullosa_Launch` + `Enemy_Medullosa_Fall` | `MedullosaBrain` al disparar: los dos a la vez. `Fall` dura 1,1 s (= `flight`) y crece hasta el final; si se cambia `flight`, regenerar el sonido con la nueva duración |
| `Enemy_Medullosa_Impact` | `MedullosaSeed` al caer (ya incluye la salpicadura: no sumar `Poison_Puddle`) |
| `Enemy_Dragonfly_Buzz_Loop` | **Bucle compartido:** suena mientras quede alguna libélula volando en la sala del jugador (contador o sondeo). Bajarlo si solo hay una lejos. `StopLoop` al morir la última o al salir de la sala |
| `Enemy_Dragonfly_Warn` | `DragonflyBrain` al empezar a marcar la línea (dura 0,7 s = `warnTime`). Si el picado se cancela, `Sfx.Stop` |
| `Enemy_Dragonfly_Dive` / `Enemy_Dragonfly_Land` | Al lanzarse y al posarse (si acaba sobre lodo, en vez de Land suena `Mud_Splash`, porque no se posa) |
| `Enemy_Bombardier_Warn` | `BeetleBrain` al girarse y avisar (0,6 s = `warnTime`) |
| `Enemy_Bombardier_Flame_Start` + `_Loop` + `_End` | Las 3 fases del lanzallamas: `Play(Start)` + `StartLoop(Loop)` al salir, `StopLoop(Loop)` + `Play(End)` al acabar (1,2 s = `sprayTime`). **Varios bombarderos a la vez comparten el bucle: contador** (StopLoop solo cuando no quede ninguno escupiendo); Start y End con intervalo ~0,1 s |
| `Enemy_Spider_FernShake_Loop` | Araña escondida: bucle solo si alguna araña escondida está a menos de ~6 u del jugador (es la pista para descubrirla). Gestor compartido |
| `Enemy_Spider_Emerge` | `SpiderBrain` al salir del helecho (por distancia, por fuego o a los 10 s). Apagar el bucle de arriba si ya no queda ninguna escondida cerca |
| `Enemy_Spider_Silk` / `Enemy_Spider_Leap` | Al lanzar seda (`SpiderSilk`) y al saltar. La telaraña que deja usa `Web_Stick` / `Web_Burn` (tanda 1) |
| `Enemy_Toad_Croak` | `alertSound` del sapo: al verte. Si croan varios a la vez, intervalo ~0,5 s |
| `Enemy_Toad_Spit` / `Enemy_Toad_BallSplat` | `ToadBrain` al escupir y `ToadMudBall` al caer |
| `Enemy_Toad_Dive` / `Enemy_Toad_Surface` | Al zambullirse (golpeado en el lodo, inmune 2 s) y al volver a asomar |
| `Enemy_Toad_Swell` → `Enemy_Toad_Burst` | Al empezar a hincharse (0,5 s = `inflateTime`) y al reventar. Si revienta, **no** suena `Enemy_Toad_Death` |
| `Trap_Carnivore_Snap` → `Trap_Carnivore_Chew` | `CarnivorousPlant`: al cerrarse sobre algo, `Snap` y enseguida `Chew` (1 s = `holdSeconds`). Si atrapa al jugador, al soltarlo `Trap_Carnivore_Release`; si atrapa a un enemigo, `Trap_Carnivore_Eat` en lugar de `Release` (y sin el `deathSound` del enemigo) |
| `Trap_Carnivore_Open` / `Trap_Carnivore_Wither` | Al reabrirse tras descansar y al secarse al limpiar la sala |
| `Trap_Mayfly_Swarm_Loop` | `MayflySwarm`: bucle mientras el enjambre persigue (activo en la sala). Contador si hay más de uno |
| `Trap_Mayfly_Sting` | Cada picada (1 por segundo) |
| `Trap_Mayfly_Burn` | Al arder (con fuego o explosión): `StopLoop` del enjambre y este sonido (dura lo que `fadeSeconds` + `emberSeconds`, 1,2 s) |

## Tanda 5: salas y escenario

**Sustituciones por piso** (hoy el código no distingue el piso 3): `Room.cs` elige
`SlabDoors ? Sounds.SlabsClose : Sounds.DoorsClose` (línea ~175) y lo mismo al abrir (~274), y toca
`Sounds.WaveStart` (~223) y `Sounds.RoomCleared` (~275). En el piso 3 deben sonar `Room_Roots_Close`,
`Room_Roots_Open`, `Room_WaveStart_Floor3` y `Room_Cleared_Floor3`. Lo limpio es un pequeño selector por piso
(por ejemplo, un método en `Room` o en `FloorConfig` que dé el sonido de puertas, oleada y sala limpia según
`FloorGenerator.CurrentFloor`), en lugar de añadir más `if` sueltos.

| Id | Dónde y cómo |
|---|---|
| `Room_Roots_Close` / `Room_Roots_Open` | Ver arriba. Cierre al empezar el combate, apertura al limpiar |
| `Room_Cleared_Floor3` | Ver arriba (junto con la apertura) |
| `Room_WaveStart_Floor3` | Ver arriba (`Room.WaveStarted`) |
| `Room_ConeChest_Open` / `Room_ConeChest_Open_Big` | `ConeChest`: al abrirse (el grande, en el Tocón hueco). El cono ámbar de la sala del ámbar no es un `ConeChest` (es `AmberBlock`, tanda 3) |
| `Room_Teleport_Wake` | `Teleporter`: al activarse (modo `Waking`, la primera vez que entras en su sala) |
| `Room_Teleport_Warp` | `Teleporter`: al usarlo desde el mapa (modo de `warp`) |
| `Room_Heart_Drop` | `RoomHearts`: cuando sale el medio corazón al limpiar la sala |
| `Room_HeartPod_Send` | E sobre el corazón con la vida llena: sale volando hacia la vaina (`HeartFlight`, 1,1 s = `Seconds`) |
| `Room_HeartPod_Take` | `HeartPod`/`HeartStorage`: E en la vaina que cura medio corazón. **Sustituye** a `Sounds.PlayerHeal` que suena ahí ahora (`HeartStorage.cs` ~224 y ~297; comprobar cuál es el de la vaina) |
| `Room_HeartPod_Empty` | E en la vaina sin corazones o con la vida llena (no pasa nada) |
| `Roach_Emerge` / `Roach_Grab` / `Roach_Hide` | `HeartThief` y `BiomassThief`: salir de la grieta, coger y esconderse |
| `Roach_Scurry_Loop` | **Bucle compartido** mientras alguna cucaracha corre (contador; en la sala Ladronas puede haber varias). StopLoop al esconderse o morir la última |
| `Roach_Hurt` / `Roach_Death` | Solo las ladronas de biomasa (`BiomassThief`), que se pueden matar. Las del corazón no se matan |
| `Critter_Scurry` | `BeetleCritter` al huir del jugador. Muy bajo; intervalo ~0,5 s para que varios no se amontonen |
| `Room_RootWall_HeartPlace` | `RootWall` al insertar el corazón morado o la semilla. **Sustituye** a `Sounds.HeartCoral` en `RootWall.cs` (~163) |
| `Room_RootWall_Open` | `RootWall` al **empezar** la animación de apertura: el desgarro del sonido cae a ~0,9 s, cuando se abre en el cuadro 8 (`OpensAtFrame`, con los tiempos de `OpenSeconds`). Si cambian esos tiempos, mover el desgarro en el generador |
| `Room_PurpleFlower_Pickup` | `PurpleFlower` al cogerla. **Sustituye** a `Sounds.HeartCoral` (~62) |
| `Room_PurpleSeed_Sprout` | Al poner la semilla: brota el corazón (el latido cae a los 0,6 s). Revisar también `SecretReward.cs` (~58), que hoy toca `HeartCoral` |
| `Room_Log_Step` | `FallenLog`: cada pisada (animación de pisada). Frecuente: intervalo ~0,25 s. `Room_Log_Bubble` con las burbujas grandes (`BigBubbleShare`) |
| `Room_Coal_Ignite` + `Room_Coal_Burn_Loop` | `CoalCell` (Veta de carbón): Ignite al prender la primera celda y **un solo bucle** mientras arda alguna (contador por sala). Durante las brasas (`EmberSeconds`, 12 s), bajar el bucle a ~40 % con `AudioManager.Instance.StartLoop(id, 0.4f)` |
| `Room_FireFront_Loop` | `FireFrontController` (Frente de fuego): bucle mientras el frente avanza; volumen según la distancia del jugador al frente si es fácil |
| `Room_Stump_Light_Loop` | `StumpLight` (Tocón hueco): bucle bajo mientras el jugador está en la sala del tocón |

Los sonidos de las salas Burbujeo (`Methane_Bubble_*`), Fumarolas (`Fumarole_*`), Tesoro hundido (lodo), Claro de
la carnívora (trampas) y Bosque en humo (ambiente de la tanda 10) ya están en otras tandas.

## Tanda 6: jefe Meganeura

Código en `Scripts/Enemies/MeganeuraBoss*.cs`, `MeganeuraEgg`, `MeganeuraNymph`, `MeganeuraStorm`. Seguir el patrón
de los jefes del piso 2 (`PlayModeSound(previo, siguiente)` en `SetMode` para cortar lo que dependa del modo al
salir de él; ver `primordia-floor2-audio.md`). Los sonidos de escena se tocan **al empezar** la escena y se cortan
con `Sfx.Stop` si se salta.

| Id | Dónde y cómo |
|---|---|
| `Boss_Meganeura_Intro` | Al empezar `IntroRoutine` (vuelo de 1,4 s + posarse). Si el intro se salta, `Sfx.Stop` |
| `Boss_Meganeura_Wings_Loop` | **Bucle del jefe:** encendido mientras vuela alto (modos de vuelo y persecución, sombra), apagado en el suelo, atrapada, cayendo o muerta. Llevarlo desde `SetMode` |
| `Boss_Meganeura_TakeOff` | `TakeOff` (0,8 s = `ClimbSeconds`) |
| `Boss_Meganeura_Mark` → `Boss_Meganeura_MarkLock` → `Boss_Meganeura_Dive` → `Boss_Meganeura_DiveImpact` | `Dive`: `Mark` al aparecer la marca ámbar (1,1 s = `MarkFollowSeconds`), `MarkLock` al fijarse en rojo (0,7 s = `MarkLockSeconds`), `Dive` al lanzarse (0,4 s) e `Impact` al tocar el suelo. En el triple picado se repite la secuencia; cortar `Mark`/`MarkLock` si se interrumpe |
| `Boss_Meganeura_TrunkStuck` | `StuckRoutine` (1,5 s = `StuckSeconds`). Si sale antes (daño, fase), `Sfx.Stop` |
| `Boss_Meganeura_Gust` | `GustRoutine` (0,7 s = `GustBlowSeconds`) |
| `Boss_Meganeura_FallBurning` | `BurnRoutine`/`FallRoutine(burning: true)`. La caída sin fuego (electrocutada) usa `Electrocuted` |
| `Boss_Meganeura_Hurt` | Cada golpe que recibe abajo (intervalo ~0,08 s; se asigna como los `Hurt` de otros jefes) |
| `Boss_Meganeura_Cry` | Al pasar a la fase 2 (65 %), antes de poner huevos (`PhaseTwoCycle` al empezar) |
| `Boss_Meganeura_EggLay` | `LayEggs`, por racimo |
| `Boss_Meganeura_Egg_Hit` / `_Egg_Break` / `_Egg_Hatch` | `MeganeuraEgg`: golpe, rotura y eclosión (sale ninfa) |
| `Boss_Meganeura_Nymph_Warn` → `_Nymph_Lash` → `_Nymph_Pull` | `MeganeuraNymph`: aviso (0,4 s = `WarnSeconds`), latigazo del labio (0,24 s) y arrastre si te alcanza |
| `Boss_Meganeura_Nymph_Hurt` / `_Nymph_Death` | Golpe y muerte de la ninfa (también al electrocutarse en una charca: intervalo ~0,1 s, pueden morir varias juntas) |
| `Boss_Meganeura_Screech` | `Rage`: el chillido con ondas al bajar furiosa (todos los huevos rotos) |
| `Boss_Meganeura_Land` | Al posarse en la ventana de daño (furiosa o tras el picado si queda en el suelo) |
| `Boss_Meganeura_StormEntry` | `StormEntry`, al empezar (con el chillido). Dura 2,8 s: a los 2,4 s ya suena la lluvia que arranca; **en el instante del primer trueno (justo después de `Wait(0.4f)`, cuando se dibuja `boltHuge`) tocar `Boss_Meganeura_Bolt_Strike` y `Boss_Meganeura_Thunder`**, y empezar el bucle `Amb_Floor3_Rain_Loop` en `storm.SetRain(true)` |
| `Amb_Floor3_Rain_Loop` | Bucle de lluvia mientras llueve (fase 3). En la muerte, `StopLoop` con fundido de unos 3 s, a la vez que `StormFade` |
| `Boss_Meganeura_Thunder` | Cada relámpago que revela su silueta (`storm.Flash` sin rayo) y con el primer trueno |
| `Boss_Meganeura_BigBolt_Warn` | `BigBoltRoutine` al aparecer la señal azul (dura 2 s = `BigChargeSeconds` 1,5 + `BigFixedSeconds` 0,5) y llega a lo más alto al caer. Luego `Bolt_Strike` |
| `Boss_Meganeura_LooseBolt_Warn` | `LooseBoltVolley` al marcar el suelo (1 s = `LooseChargeSeconds`): **un solo sonido por andanada**, no por rayo. Luego `Bolt_Strike` con intervalo ~0,06 s (caen varios a la vez) |
| `Boss_Meganeura_Electrocuted` | `ShockRoutine` (el rayo grande la alcanza; 0,8 s + caída) |
| `Boss_Meganeura_Puddle_Electric_Loop` | **Bucle compartido** mientras haya alguna charca electrificada (`PuddleLive`, 5,5 s cada una): contador |
| `Boss_Meganeura_Ring_Loop` | `RingRoutine`: bucle mientras dura la corriente circular (4 s = `RingSeconds`) |
| `Boss_Meganeura_Sigillaria_Burn` | Una Sigillaria quemada hace de pararrayos (`BossSigillaria`): con el `Bolt_Strike` que la alcanza |
| `Boss_Meganeura_Death` | Al empezar `DeathRoutine`. Dura 2,8 s: el ala se rompe a los 0,6 s (`RiseBeforeFall`) y el chapuzón cae a los 2,2 s (`DeathDuration`) |
| `Boss_Meganeura_StormFade` | Después de la muerte, cuando la tormenta amaina (3 s = `CalmSeconds`) |

La música de la Meganeura (normal y tormenta) va en la tanda 10.

## Tanda 7: jefe Arthropleura

Código en la rama `feature/boss-arthropleura` (sin fusionar en `feature/floor-3` cuando se escribió esto):
`Scripts/Enemies/ArthropleuraBoss*.cs`. El caparazón **siempre** es invulnerable: lo que choca contra él suena
`Shell_Hit` (sordo), y solo la carne de las placas abiertas suena `Hurt`.

| Id | Dónde y cómo |
|---|---|
| `Boss_Arthropleura_Intro` | Al empezar la presentación (`ArthropleuraBoss.Intro`, temblor de 1,5 s = `IntroShakeSeconds`). `Sfx.Stop` si se salta |
| `Boss_Arthropleura_Rustle_Loop` | **Fase 1:** bucle mientras rodea el claro oculto. Apagar al pasar a la fase 2 |
| `Boss_Arthropleura_Legs_Loop` | **Fases 2 y 3:** bucle mientras se mueve (no cuando está aturdido, enroscado ni muerto). Durante la rueda lo sustituye `Roll_Loop` |
| `Boss_Arthropleura_Charge_Warn` → `Boss_Arthropleura_Charge` | `ArthropleuraBoss.Charge`: el aviso al asomar (1,4 s = `WarnSeconds`, el siseo final coincide con `LockBefore`) y la embestida al lanzarse |
| `Boss_Arthropleura_EdgeBreak` | Al abrir la brecha en el borde (`ArthropleuraBoss.Hunt`, con el `BreachShake`) |
| `Boss_Arthropleura_Shell_Hit` | Golpes y proyectiles contra el caparazón cerrado. Intervalo ~0,08 s (muchas piezas a la vez) |
| `Boss_Arthropleura_Plate_Open` / `Boss_Arthropleura_Plate_Close` | `ArthropleuraBoss.Fire`: al abrirse y cerrarse las placas de un tramo (0,36 s = `PlateTransitionSeconds`). Si se abren varios tramos a la vez, intervalo ~0,15 s |
| `Boss_Arthropleura_Hurt` | Daño en la carne de una placa abierta. Intervalo ~0,08 s |
| `Boss_Arthropleura_Ignite` | Al prender un segmento por primera vez (odre, helecho o el fuego de la fase 3) |
| `Boss_Arthropleura_Fuse_Loop` | Bucle mientras el fuego corre por el cuerpo (la mecha avanza 0,4 s por pieza = `FuseSeconds`). Apagar cuando no quede ninguna pieza ardiendo |
| `Boss_Arthropleura_Hiss` | Al arder (con `Ignite`, intervalo ~2 s para que no se repita), al quedar aturdido y al pasar a la fase 3 (35 %) |
| `Boss_Arthropleura_WallCollapse` | `ArthropleuraBoss.Phase2` al empezar la transición del 70 % (dura 3,2 s = `TransitionSeconds`; el derrumbe va de 0,8 a 2,2 s = `CollapseStart` + `CollapseSeconds`) |
| `Boss_Arthropleura_Stomp_Warn` → `Boss_Arthropleura_Stomp_Wave` ×3 | Aviso al alzarse (0,9 s = `StompWarnSeconds`) y **un `Stomp_Wave` por cada onda** (cada 0,3 s = `WaveGap`) |
| `Boss_Arthropleura_TreeFall` | `ArthropleuraBoss.Trees`: al derribar un árbol (el golpe final cae ~0,6 s antes del final del sonido, cuando el tronco toca el suelo) |
| `Boss_Arthropleura_Stunned` | Al embestir un árbol y quedar aturdido (3 s = `StunSeconds`), junto con `TreeFall` y `Hiss` |
| `Boss_Arthropleura_EatFern` | Al comerse un helecho (si arde, además `Ignite`) |
| `Boss_Arthropleura_Douse` | `ArthropleuraBoss.Mud`: al meterse en la charca y apagarse. Apagar `Fuse_Loop` |
| `Boss_Arthropleura_Ring_Close` | `ArthropleuraBoss.Phase3`: al empezar a cerrarse el anillo (2,2 s = `RingContractSeconds`). Durante la formación (4,5 s) y la espera (3,5 s) basta `Legs_Loop` |
| `Boss_Arthropleura_Squeeze` / `Boss_Arthropleura_Expel` | Te pilla dentro y te aprieta; te expulsa (`ExpelDistance`) |
| `Boss_Arthropleura_Coil` | `ArthropleuraBoss.Phase4`: al enroscarse en rueda (último 10 %) |
| `Boss_Arthropleura_Roll_Loop` | Bucle mientras rueda. Apagar al chocar y aturdirse, volver a encender al rodar de nuevo |
| `Boss_Arthropleura_Roll_Crash` | Cada choque contra una pared (rebote y aturdimiento) |
| `Boss_Arthropleura_Death` | Al empezar `ArthropleuraBoss.Death`: los crujidos siguen a las piezas que mueren (la primera a 0,3 s = `FirstDeath`, cada 0,09 s = `DeathGap`); dura 3,2 s. Apagar todos los bucles del jefe |
| `Boss_Arthropleura_Smolder_Loop` | Después de morir, bucle muy bajo mientras el jugador esté cerca del cadáver humeante |

## Tanda 8: minijefe Megarachne

Código en `Scripts/Enemies/MegarachneBoss*.cs` (`Attacks`, `Cables`, `Death`, `Fire`, `Intro`, `Rage`, `Sac`),
`MegarachneBossNest`, `MegarachneBossSac`, `MegarachneBossWeb`, `MegarachneBrood`, `MegarachneCorpse`.
`MegarachneCorpse.cs` (~157) toca hoy `Sounds.HeartCoral` con la recompensa: sustituirlo por `Boss_Megarachne_Cocoon_Open`.

| Id | Dónde y cómo |
|---|---|
| `Boss_Megarachne_Intro` | Al empezar `MegarachneBoss.Intro` (salida del embudo de 0,88 s = `EmergeSeconds`, ojos de 0,7 s = `EyesSeconds`, amenaza). `Sfx.Stop` si se salta |
| `Boss_Megarachne_CableRun_Loop` / `Boss_Megarachne_Walk_Loop` | **Bucles excluyentes:** uno u otro según esté en un cable o en el suelo; ninguno quieta, saltando, aturdida o muerta |
| `Boss_Megarachne_Climb` / `Boss_Megarachne_Drop` | Subir al cable (0,4 s = `ClimbSeconds`) y bajar (0,48 s = `DropSeconds`) |
| `Boss_Megarachne_Jump_Warn` → `Boss_Megarachne_Jump_Land` | Aviso de salto (1 s = `JumpWarning`; el siseo final coincide con `JumpLocked`) y caída (ventana de 1,5 s) |
| `Boss_Megarachne_SilkVolley` | Al empezar la animación de la ráfaga: las bolitas salen a los 0,36 s (`SilkRelease`), ya incluido en el sonido |
| `Boss_Megarachne_Silk_Splat` | Cada bolita que cae y deja telaraña; caen 5-7 casi a la vez: intervalo ~0,05 s |
| `Boss_Megarachne_Net_Warn` → `Boss_Megarachne_Net_Drop` → `Boss_Megarachne_Net_Trap` | Aviso de la red (1,2 s = `NetWarning`), caída (0,4 s = `NetFallSeconds`) y, si atrapa al jugador, `Net_Trap` (1 s = `TrappedSeconds`) |
| `Boss_Megarachne_Sac_Weave` | Al tejer el saco (0,88 s = `SacWeaveSeconds`) |
| `Boss_Megarachne_Sac_Tremble` | `MegarachneBossSac`: al empezar a temblar (1 s = `TrembleSeconds` antes de reventar). Si se rompe antes, `Sfx.Stop` |
| `Boss_Megarachne_Sac_Hit` / `_Sac_Break` / `_Sac_Burst` | Golpe al saco; roto por el jugador (sin crías); reventado (salen las crías) |
| `Boss_Megarachne_Brood_Loop` | **Bucle compartido** mientras quede alguna cría viva (`MegarachneBrood`; contador) |
| `Boss_Megarachne_Brood_Bite` / `_Brood_Death` | Mordisco y muerte de cada cría (intervalo ~0,08 s en la muerte, mueren juntas) |
| `Boss_Megarachne_Cable_Weave` | Al tejer un cable nuevo (3 s = `ReweaveSeconds`): el hilo fino vulnerable cruza el suelo. Si se interrumpe (lo cortan o la dañan), `Sfx.Stop` |
| `Boss_Megarachne_Cable_Ignite` + `Boss_Megarachne_Cable_Fuse_Loop` | `MegarachneBoss.Fire`: Ignite al prender un cable y bucle mientras el fuego corra por algún cable (contador: puede haber dos ardiendo) |
| `Boss_Megarachne_Cable_Snap` | El cable quemado se rompe y queda el muñón |
| `Boss_Megarachne_Fall_Stunned` | El frente de fuego la alcanza en el cable: cae y queda aturdida (3 s = `StunSeconds`) |
| `Boss_Megarachne_Hurt` | Daño en el suelo (intervalo ~0,08 s) |
| `Boss_Megarachne_Rage_Screech` | `MegarachneBoss.Rage` al 50 %: el chillido dura exactamente 2,5 s (`ScreechSeconds`). Durante el enfado se puede tocar `CableRun_Loop` algo más agudo (`pitch` 1,1) por la velocidad ×1,4 |
| `Boss_Megarachne_Nest_Wither` → `Boss_Megarachne_Nest_Burn` → `Boss_Megarachne_Web_Collapse` | `MegarachneBossNest`/`MegarachneBossWeb`: el árbol se seca, el Nido arde una vez y la red de la copa se cae, cada uno en su momento |
| `Boss_Megarachne_Death` | Al empezar `MegarachneBoss.Death` (cae panza arriba y encoge las patas). Apagar todos los bucles |
| `Boss_Megarachne_Cables_Drop` | Los cables se descuelgan al morir (5 tañidos que bajan y caen, separados ~0,15 s) |
| `Boss_Megarachne_Cocoon_Fall` / `Boss_Megarachne_Cocoon_Open` | `MegarachneCorpse`: el capullo cae (0,9 s = `CocoonFallSeconds`) y se abre con el contenedor de corazón |

## Tanda 9: jefe secreto Lepidodendron ancestral

Código en `Scripts/Enemies/LepidodendronBoss*.cs` (`Attacks`, `Cones`, `Death`, `Intro`, `Reward`, `Walls`),
`LepidodendronWall`, `LepidodendronBossWallIgnition`, `LepidodendronCone`, `LepidodendronReward` (las ramas
`codex/lepido-*` estaban sin fusionar cuando se escribió esto). `LepidodendronBoss.Reward.cs` toca hoy
`Sounds.HeartCoral` en tres sitios (~96, ~117, ~266): sustituirlos por `Heart_Pickup`, `PurpleSpore_Pickup` y
`Relic_Pickup` según cuál sea cada uno.

**El desplome (6,6 s = `CollapseSeconds`) va en tres sonidos**, porque entero pasaría de 300 KB:
1. `Boss_Lepidodendron_Death_Crack` al empezar (vetas 0,7 s + grietas 0,9 s = `VeinsSeconds` + `CrackSeconds`).
2. `Boss_Lepidodendron_Death_Fall` cuando empieza la caída (cuadro 0 de `Lepido_Death_Fall`, a ~1,6 s): el golpe del
   sonido cae a 1,77 s, la suma de `FallSteps` hasta `FallImpactFrame` (12). Si cambian `FallSteps`, mover el golpe en
   el generador.
3. `Boss_Lepidodendron_LightColumn` cuando entra la columna de luz por la copa rasgada (tras el impacto).

| Id | Dónde y cómo |
|---|---|
| `Boss_Lepidodendron_Intro` | Al empezar la presentación (paneo de 1,5 s = `IntroPanSeconds` y espera). `Sfx.Stop` si se salta |
| `Boss_Lepidodendron_Creak_Loop` | Bucle bajo mientras el jugador esté en la sala del árbol y el jefe viva |
| `Boss_Lepidodendron_RootWall_Hit` / `_KnotHit` | `LepidodendronWall`: golpe normal y golpe en nudo débil (×2). Intervalo ~0,06 s |
| `Boss_Lepidodendron_RootWall_Ignite` + `_RootWall_Burn_Loop` | `LepidodendronBossWallIgnition`: al prender (también por el cono del bombardero) y bucle mientras arda alguna pared |
| `Boss_Lepidodendron_RootWall_Retract` / `_RootWall_Crumble` | La pared llega a cero: se retira, o se desmorona si estaba carbonizada del todo (uno u otro) |
| `Boss_Lepidodendron_Wave_Spawn` | `LepidodendronBoss.Walls` al salir enemigos (destello de 0,25 s = `SpawnFlashSeconds`); varios a la vez: intervalo ~0,1 s |
| `Boss_Lepidodendron_SporeArc` | Al lanzar esporas en arco por encima de las paredes |
| `Boss_Lepidodendron_SporeRain_Warn` → `_Spore_Impact` | Lluvia de esporas: aviso al aparecer las marcas (dura 1,2 s = `SporeDelay`) y un impacto por espora (intervalo ~0,05 s) |
| `Boss_Lepidodendron_Phase_Final` | Al llegar al centro de la arena (fase final, `EngageMargin`) |
| `Boss_Lepidodendron_Trunk_Hit` | Golpes al tronco en la fase final (−80 %). Intervalo ~0,08 s |
| `Boss_Lepidodendron_Cone_Warn` → `_Cone_Descend` → `_Cone_Slam` → `_Cone_Lift` | `LepidodendronCone`: aviso (1,5 s = `WarningSeconds`), sombra fija y bajada (0,72 + 0,35 s = `ShadowSeconds` + `DescentSeconds`; el sonido dura 1,1 s), golpe y subida (1 s = `LiftSeconds`) |
| `Boss_Lepidodendron_Armor_Hit` / `_Armor_Break` | Golpe a la armadura de ramas y rotura (por golpes o fuego) |
| `Boss_Lepidodendron_Cone_Hurt` / `_Cone_Break` | Daño al cono expuesto y rotura del cono |
| `Boss_Lepidodendron_RootLash_Warn` → `_RootLash` | Latigazo: aviso (0,8 s = `WhipWarning`) y golpe |
| `Boss_Lepidodendron_SpikeRing_Warn` → `_SpikeRing` | Corro de púas: aviso (0,78 s = `SpikesWarning`) y púas |
| `Boss_Lepidodendron_Corner_Warn` → `_Corner_Strike` | Castigo en las esquinas (`AttackKind.Corners`): **un solo aviso** para las dos esquinas (1 s = `CornerWarning`) y un golpe |
| `Boss_Lepidodendron_Methane_Warn` | `LepidodendronBoss.Cones`: nubes de metano junto al tronco (1 s = `MethaneWarning`). Al prenderse, `Methane_Explosion` (tanda 1) y además `Tree_Shake` |
| `Boss_Lepidodendron_Tree_Shake` | El árbol se sacude y bajan los conos (`ConeShakeSeconds`) |
| `Boss_Lepidodendron_Death_Crack` / `_Death_Fall` / `_LightColumn` | Ver arriba. Apagar `Creak_Loop` y `RootWall_Burn_Loop` al empezar el desplome |
| `Boss_Lepidodendron_Reward_Descend` | Al empezar a bajar la recompensa por la luz (empieza a 0,4 s = `RewardStartDelay` y baja en 1,8 s = `RewardFallSeconds`; el latido final cae a los 2 s) |
| `Boss_Lepidodendron_Heart_Pickup` / `_PurpleSpore_Pickup` | Coger el corazón permanente / la Espora morada |
| `Boss_Lepidodendron_Stump_Enter` | E en la grieta de luz del tocón (entras en su sala) |
| `Boss_Lepidodendron_Relic_Pickup` | Coger la reliquia Raíz ancestral del pedestal (con su presentación) |

## Tanda 10: música y ambientes

Todo pasa por `Scripts/Audio/MusicDirector.cs`, que hoy solo conoce los pisos 1 y 2. Cambios necesarios:

1. **Pistas por tramo** (`FloorTracks()`): hoy, con `floorNumber >= 2`, elige por `band` las del piso 2. Añadir el
   caso `floorNumber >= 3` con `Room.DepthBand` 0/1/2 → `Music_Floor3_Green_*`, `Music_Floor3_Smoke_*`,
   `Music_Floor3_Burnt_*` (exploración y combate). El fundido de ~3 s al cambiar de tramo (`BandFade`) ya existe;
   comprobar que `FloorBuilder` pone `DepthBand` en el piso 3 por tercios del camino, como en el piso 2.
2. **Ambiente por tramo** (`FloorAmbience`): en el piso 3, banda 0 → `Amb_Floor3_Loop` (ya existe), 1 →
   `Amb_Floor3_Smoke_Loop`, 2 → `Amb_Floor3_Burnt_Loop`. Añadir los ids nuevos a la lista `Ambiences` (si no, no se
   apagan al cambiar). La sala del ámbar suena siempre con `Amb_Floor3_Amber_Loop` (igual en todos los tramos).
3. **Sala de jefe** (`BossAmbience`): en el piso 3, `Amb_Floor3_BossRoom_Loop`.
4. **Música de jefe** (`BossTrack`): `MeganeuraBoss` → `Music_Boss_Meganeura`, `ArthropleuraBoss` →
   `Music_Boss_Arthropleura`, `MegarachneBoss` → `Music_Boss_Megarachne`, `LepidodendronBoss` →
   `Music_Boss_Lepidodendron`. El Lepidodendron usa `Music_Sting_SecretBoss` en lugar de `Music_Sting_BossIntro`.
5. **Cambio de pista por fase** (como `OnAnomalocarisMode`, suscribiéndose al evento de fase de cada jefe; si el
   jefe no tiene uno, añadir un evento `PhaseChanged` pequeño, sin tocar su lógica):
   - Meganeura: al empezar `StormEntry` (fase 3) → `Music_Boss_Meganeura_Storm`. Al morir, cortar con fundido largo
     (la tormenta amaina con `StormFade`).
   - Arthropleura: al entrar en la fase 3 (35 %, el anillo) → `Music_Boss_Arthropleura_Coil` (la misma pista sigue
     en la rueda del último 10 %).
   - Megarachne: al enfadarse (50 %, `MegarachneBoss.Rage`) → `Music_Boss_Megarachne_Rage`; durante los 2,5 s del
     chillido conviene bajar la música (`Music.Stop(0.4f)` y volver a `Play` al acabar).
   - Lepidodendron: al llegar al centro (fase final) → `Music_Boss_Lepidodendron_Final`. Durante el desplome (6,6 s)
     parar la música con fundido: la escena tiene su propio sonido.
6. **Reliquia**: al coger la Raíz ancestral, `Music.Stinger(Music_Sting_Relic, resume: true)` (como los fragmentos
   del piso 1), junto con `Boss_Lepidodendron_Relic_Pickup`.
7. **Sala de paso del piso 3**: su sonido (ambiente del piso 4 y fundido) queda para el final, cuando se diseñe la
   música del piso 4 (decisión de la usuaria). `NextAmbience` del piso 3 sigue siendo `null` hasta entonces.

Las capas de cada tramo duran exactamente lo mismo en muestras (lo comprueba el lint), así que el juego puede
reproducirlas a la vez y fundir una con otra como en los pisos 1 y 2.
