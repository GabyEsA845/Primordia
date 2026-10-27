# Encargo: música y efectos de sonido de Primordia (piso 2, océano cámbrico)

Segundo encargo de audio, con el mismo sistema, formatos y reglas que el del piso 1 (`PROMPT.md`, «Formato y entrega»).
Se genera con `generar_audio.py`, desde `efectos_piso2.py` y `musica_piso2.py`, que usan los instrumentos de
`instrumentos_piso2.py` (aprobados en `muestrario_piso2.html`).

## Estilo sonoro

- **El océano entero frente a la gota del piso 1:** grande, abierto y hondo. Mantiene la base chiptune orgánica
  (nada de metal, pólvora ni armas de fuego), pero la voz principal es la **marimba de conchas** en lugar de las
  campanas, con **canto de las profundidades**, **pads que ondulan**, **percusión de mar** (quitina, piedra,
  madera, conchas) y **eco largo**.
- **Tonalidad común del juego: re.** Cada franja cambia el modo: Someras en re dórico, Arrecife en re menor
  natural y Abismo en re frigio; el laboratorio, en re menor.
- **Paleta por familia**, como en el piso 1. El jugador conserva sus sonidos base. Las Bocas y mutaciones del
  océano suenan a agua grande, concha y quitina. Los enemigos van de graves a medios: caparazones, gelatina
  translúcida, arena y esponja. Los jefes son enormes: el Anomalocaris es un depredador que acecha en la oscuridad
  (agua desplazada, chasquidos de apéndices) y el Euriptérido, coraza, piedra y veneno.

## Música

Una música por franja, cada una con **dos capas sincronizadas** (exploración y combate) como en el piso 1: mismo
tempo, modo y duración exacta en muestras. Al pasar de una franja a otra, la música cambia al entrar en la primera
sala de esa franja (dentro de un combate no cambia) con un **fundido cruzado de unos 3 s**: la antigua se va
desvaneciendo mientras entra la nueva (decidido por el usuario). Como todo está en re, las dos casan mientras suenan juntas. Para no pasar de unos 12 MB, las capas duran menos
que las del piso 1.

| Archivo | Uso | Duración | Ambiente |
|---------|-----|----------|----------|
| `Music_Floor2_Shallows_Explore.ogg` | Someras (primer tercio), sin combate (bucle, capa A) | 24 compases a 100 BPM (57,6 s) | Re dórico, luminoso: agua clara con rayos de luz. Marimba en arpegios, melodía de marimba con eco, pads claros, conchas suaves, burbujitas. Como el esbozo aprobado |
| `Music_Floor2_Shallows_Combat.ogg` | Someras con las puertas cerradas (bucle, capa B, en sincronía con la A) | Igual que la A | La misma armonía con tambores de madera, bajo rítmico, arpegios cortos de marimba y algo de batería chiptune |
| `Music_Floor2_Reef_Explore.ogg` | Arrecife (segundo tercio), sin combate (bucle, capa A) | 24 compases a 108 BPM (53,3 s) | Re menor natural, lleno de vida: percusión de conchas y quitina con síncopas, bajo pulsado, ostinato de marimba en 3+3+2 y flauta con eco corto |
| `Music_Floor2_Reef_Combat.ogg` | Arrecife con las puertas cerradas (bucle, capa B) | Igual que la A | Más energía: tambor hondo, bajo más denso, la flauta más aguda, chiptune |
| `Music_Floor2_Abyss_Explore.ogg` | Abismo (último tercio), sin combate (bucle, capa A) | 16 compases a 80 BPM (48 s) | Re frigio, enorme y lento: canto de las profundidades delante, pads oscuros, subgrave discreto, latido, sonar con eco, burbujas grandes |
| `Music_Floor2_Abyss_Combat.ogg` | Abismo con las puertas cerradas (bucle, capa B) | Igual que la A | Pulso grave insistente, tambor hondo, latido doble, el canto más tenso |
| `Music_Exchange.ogg` | Sala de cambio: el laboratorio de Darwin con la cápsula (bucle). Sirve para todos los pisos | 16 compases de 6/8 (38,4 s) | Misterioso y curioso: armónica de cristal, marimba suave, la cápsula burbujeando y un tic de laboratorio. Como el esbozo aprobado |
| `Music_Boss_Anomalocaris.ogg` | Jefe Anomalocaris, acecho y cerco (fases 1 y 2) (bucle) | ~64 s | Acecho en la oscuridad: bajo insistente, pulsos de sonar, cuerdas que tiemblan, marimba grave. Tensión contenida |
| `Music_Boss_Anomalocaris_Frenzy.ogg` | Anomalocaris, frenesí (fase 3, tras la escena de los ojos) (bucle) | ~40 s | La misma idea, más rápida y desatada: tambores hondos, ostinato rápido. Durante la escena de los ojos la música se apaga |
| `Music_Boss_Eurypterid.ogg` | Jefe Euriptérido, fase acorazada y fase blanda (bucle) | ~64 s | Pesado y pétreo: percusión de roca y quitina, golpes como derrumbes, bajo que avanza despacio |
| `Music_Boss_Eurypterid_LastStand.ogg` | Último aliento: el bullet hell de 25 s en tres tramos (8 + 8 + 9 s). **No es bucle**: dura **exactamente 25 s** y empieza con la primera salva | 25,0 s | Clímax en tres tramos que suben de intensidad (anillos → espirales → todo a la vez) y corte seco al final, cuando cae |

La tienda usa `Music_Shop` en todos los pisos (decidido). Victoria, derrota, presentación de jefe y evolución de
linaje reutilizan los fragmentos del piso 1.

## Ambiente

| Archivo | Uso | Duración | Notas |
|---------|-----|----------|-------|
| `SFX_Amb_Floor2_Abyss_Loop.wav` | Debajo de la música en el Abismo, en lugar de `Amb_Floor2_Loop` (bucle, muy bajo) | 20–30 s | El mismo mar, más hondo y oscuro: rumor más grave, nieve marina (chasquidos finísimos), corrientes lejanas, algún sonar perdido. Sin ritmo |
| `SFX_Amb_Floor2_BossRoom_Loop.wav` | Salas de los dos jefes del piso 2 antes y después del combate (bucle) | 15–20 s | Mar profundo y cerrado, con crujidos de roca lejanos y un zumbido grave de amenaza |

## Efectos de sonido

En cada tabla, "Cuándo suena" dice qué lo dispara (entre paréntesis, el evento o efecto del código). Algunos
sustituyen a un sonido prestado del piso 1, como se indica.

### Mutaciones del piso 2: Boca

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Mouth_Cnidocytes_Shot` | 3 | 0,08 s | Disparo de los Cnidocitos, 3 por segundo (`Attack_Cnidocytes*`) | Arpón diminuto que sale despedido: "tsk" elástico, húmedo y agudo |
| `SFX_Mouth_Cnidocytes_Fan` | 1 | 0,2 s | Ramillete (III): cada 4º disparo salen 3 en abanico | El mismo arpón triple, abierto, con un poco más de cuerpo |
| `SFX_Mouth_JawPlates_Bite` | 3 | 0,15 s | Mordisco de las Placas mandibulares, 2 por segundo (`Attack_JawPlates*`) | Placas de hueso que se cierran: "clonk" seco y duro, más óseo que las Mandíbulas |
| `SFX_Mouth_JawPlates_BiteHit` | 2 | 0,15 s | El mordisco alcanza a un enemigo | Crujido de coraza que cede |
| `SFX_Mouth_JawPlates_Guillotine` | 1 | 0,3 s | Guillotina (III): cada 4º mordisco, doble, triple de daño y aturde (`Effect_JawPlates_Guillotine`) | Golpe doble contundente, como una prensa que cae |
| `SFX_Status_Shatter` | 1 | 0,25 s | Quebrar: el enemigo recibe +25 % de daño (`Effect_Status_Shatter`) | Grieta que recorre un caparazón |
| `SFX_Mouth_ArmCrown_Cast` | 1 | 0,25 s | Aparece el remolino de la Corona de brazos en el cursor | Agua que empieza a girar: "fwuu" ascendente |
| `SFX_Mouth_ArmCrown_Whirl_Loop` | 1 | 1 s | Mientras dura el remolino (2–2,5 s, `Effect_ArmCrown_Whirl`) | Remolino que gira y succiona, sin ritmo marcado; el juego lo para al acabar |
| `SFX_Mouth_ArmCrown_Tide` | 1 | 0,6 s | Marea (III): el remolino estalla y empuja (`Effect_ArmCrown_Tide`) | Ola que rompe hacia fuera |

### Mutaciones del piso 2: Extremidades, Piel y Órgano

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Limbs_Tail_Dodge` | 2 | 0,3 s | Esquiva con la Cola propulsora (sonido de esquiva de la mutación) | Chorro de agua potente, propulsión con un coletazo |
| `SFX_Limbs_Tail_Bubbles` | 1 | 0,3 s | La esquiva suelta sus burbujas (`TailBubbles.Released`) | Racimo de burbujas grandes que se quedan flotando |
| `SFX_Limbs_Tail_BubblePop` | 2 | 0,15 s | Una burbuja de la cola estalla, daña y empuja (`Effect_TailPropulsor_Pop`) | Pop grave y contundente |
| `SFX_Limbs_Hallucigenia_Place` | 2 | 0,15 s | Se clavan las púas en el suelo (`SpineTraps.Placed`) | Púas que se hunden en la arena: "tsk-tsk" |
| `SFX_Limbs_Hallucigenia_Strike` | 2 | 0,1 s | Un enemigo pisa una púa (`GroundSpine.Struck`) | Pinchazo seco |
| `SFX_Limbs_Hallucigenia_Launch` | 1 | 0,25 s | Erizo (III): las púas salen disparadas (`GroundSpine.Shot`) | Púas que se sueltan de golpe y silban |
| `SFX_Skin_Trilobite_Bounce` | 2 | 0,15 s | Las Placas de trilobite rebotan un proyectil (`PlatesBounce.Bounce`; ahora suena `Organ_Shockwave_ProjectileBreak`) | "Tink" de caparazón, rebote limpio |
| `SFX_Skin_Trilobite_Roll` | 1 | 0,3 s | Enrollarse (III): esquiva enrollado que rebota todo | Caparazón que se enrosca, clic de placas en cadena |
| `SFX_Skin_Wiwaxia_Volley` | 1 | 0,3 s | Salen las escamas de Wiwaxia (`ScleriteVolley.Released`; ahora suena `Skin_Quills`) | Escamas que se sueltan, como un abanico de conchas |
| `SFX_Skin_Wiwaxia_Orbit` | 1 | 2,0 s | Órbita (III): las escamas giran 2 s alrededor antes de salir | Zumbido que gira alrededor, con tintineo de escamas |
| `SFX_Organ_BroodSac_Open` | 1 | 0,6 s | Se abre el Saco de crías y salen las larvas (`Effect_BroodSac_Open`) | Bolsa que revienta suave y larvas que se escabullen |
| `SFX_Organ_BroodSac_LarvaBite` | 3 | 0,08 s | Una larva muerde (`Larva.AnyBit`) | Mordisquito agudo; muy bajo, suena mucho |
| `SFX_Organ_BroodSac_Split` | 1 | 0,2 s | Enjambre (III): la larva que mata se divide (`Effect_BroodSac_Split`) | "Plip-plip" de dos larvas |
| `SFX_Organ_CompoundEyes_On` | 1 | 0,6 s | Ojos compuestos: todo va al 60 % salvo tú (`CompoundEyesEffect.Started`; ahora suena `Player_AbilityReady`) | El tiempo se ralentiza: barrido que baja de tono y se ensancha |
| `SFX_Organ_CompoundEyes_Off` | 1 | 0,4 s | Termina el efecto (`CompoundEyesEffect.Ended`) | Lo contrario: el tiempo vuelve, barrido que sube |

### Mutaciones especiales (almejas grandes)

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Special_Flagella_Lash` | 2 | 0,5 s | Latigazo de los Flagelos de Leanchoilia; el daño entra a los 0,4 s (`FlagellaLash`; ahora suena `Skin_Thorns`) | Silbido de látigo que crece y chasquido justo a los 0,4 s |
| `SFX_Special_Flagella_Ready` | 1 | 0,15 s | El latigazo vuelve a estar listo (destello azul verdoso) | Brillo breve y discreto |
| `SFX_Special_Cuticle_Reflect` | 2 | 0,2 s | La Cutícula de Marrella refleja un proyectil (ahora suena `Organ_Shockwave_ProjectileBreak`) | Destello iridiscente: barrido cristalino que sube |
| `SFX_Special_Cuticle_Stun` | 1 | 0,2 s | Refleja un golpe cuerpo a cuerpo y aturde (ahora suena `Limbs_Legs_Push`) | El mismo destello con un golpe seco debajo |
| `SFX_Special_Bud_Spawn` | 1 | 0,6 s | Brota la copia de la Yema clonal | Brote acuoso que se infla, eco de tu propia burbuja |
| `SFX_Special_Bud_Dissolve` | 1 | 0,5 s | La copia se disuelve en burbujas | Burbujas que se deshacen hacia arriba |
| `SFX_Special_Unstable_Crack` | 1 | 0,25 s | Inestable: el medio corazón extra se rompe con un destello dorado | Cristal dorado que se agrieta |

### Linajes del piso 2

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Lineage_Carrion_Eat` | 2 | 0,25 s | Carroña: el jugador toca unos restos y gana daño (`CarrionBenefit`; ahora suena `Biomass_Pickup_Big`) | Bocado carroñero, crujiente y algo sucio. El juego sube el tono con cada carga (×1, ×2, ×3) |

Los toques de linaje del piso 2 reutilizan sonidos: Placodermo (`Player_Heal`), Burgessomedusa y Anémona
(`Status_Poison`), Wiwaxia (`Lineage_Barrier`), Trilobite (`Skin_Trilobite_Bounce`).

### Enemigos del piso 2

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Enemy_Shell_Hit` | 3 | 0,1 s | El trilobite recibe daño | Golpe sobre caparazón: "tok" seco con algo de agua |
| `SFX_Enemy_Jelly_Hit` | 3 | 0,1 s | La medusa azul recibe daño | "Plop" gelatinoso y agudo, translúcido |
| `SFX_Enemy_Sponge_Hit` | 2 | 0,1 s | La esponja Vauxia recibe daño | Golpe sordo y esponjoso, sin brillo |
| `SFX_Enemy_Trilobite_Warn` | 1 | 0,6 s | Aviso antes de embestir: rasca el suelo (aviso de 0,6 s) | Patitas que escarban, tensión que sube hasta la arrancada |
| `SFX_Enemy_Trilobite_Charge` | 1 | 0,3 s | Sale embistiendo | Arrancada rápida de patas y agua |
| `SFX_Enemy_Trilobite_Crash` | 2 | 0,4 s | Choca contra una pared y queda panza arriba (aturdido 1 s) | Golpe de caparazón contra roca y patitas que patalean |
| `SFX_Enemy_Trilobite_Death` | 2 | 0,4 s | Muere | Caparazón que se parte y burbujas |
| `SFX_Enemy_Jelly_Ring` | 2 | 0,3 s | La medusa suelta su anillo de 8, cada 3 s (`BlueJellyBrain.RingFired`) | Latido de la campana: "fwoom" blando que se abre en anillo |
| `SFX_Enemy_Jelly_Death` | 2 | 0,4 s | Muere | Se deshace en burbujas finas, translúcido |
| `SFX_Enemy_Ottoia_Warn` | 1 | 0,8 s | Onda en la arena y círculo antes de morder (aviso de 0,8 s) | Arena que se levanta desde abajo, crece hasta el mordisco |
| `SFX_Enemy_Ottoia_Bite` | 2 | 0,25 s | Sale y muerde (`OttoiaBrain.Bit`) | Emerge de golpe: arena que salta y dentellada |
| `SFX_Enemy_Ottoia_Sink` | 1 | 0,4 s | Se vuelve a hundir | Arena que se traga al gusano |
| `SFX_Enemy_Ottoia_Death` | 1 | 0,4 s | Muere | Gusano que se desinfla, chapoteo de arena |
| `SFX_Enemy_Vauxia_Pulse` | 1 | 0,5 s | La esponja cura y da escudo cada 3 s (`VauxiaBrain.Pulsed`) | Pulso cálido y hueco, como una respiración que se expande |
| `SFX_Enemy_Shield_Block` | 2 | 0,12 s | El escudo de la esponja absorbe un golpe | Golpe amortiguado contra una burbuja dura |
| `SFX_Enemy_Vauxia_Death` | 1 | 0,5 s | La esponja muere | Esponja que se exprime y suelta agua |
| `SFX_Enemy_Pikaia_Dash` | 2 | 0,25 s | El cardumen pasa en formación | Ráfaga de varios cuerpecillos cortando el agua |
| `SFX_Enemy_Pikaia_Death` | 2 | 0,2 s | Muere una Pikaia | Versión corta y aguda de la muerte |
| `SFX_Enemy_PitFall` | 1 | 0,6 s | Un enemigo empujado cae a una fosa | Silbido que baja y se pierde en lo hondo |

Los que no aparecen (golpe de la Ottoia y de la Pikaia, proyectil que choca) usan los del piso 1.

### Jefe: Anomalocaris

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Anomalocaris_Bubble` | 3 | 0,08 s | Estela y muro de burbujas (salen cada 0,12 s en el cerco) | Burbuja grave muy corta; muy baja, suena sin parar |
| `SFX_Boss_Anomalocaris_BubblePop` | 3 | 0,08 s | Una burbuja revienta (`Effect_AnomalocarisBubble_Pop`) | Pop grave y breve |
| `SFX_Boss_Anomalocaris_Warn` | 1 | 0,7 s | Aviso del zarpazo: se echa atrás y abre los apéndices (0,7 s) | Apéndices que se abren con chasquidos y tensión que sube |
| `SFX_Boss_Anomalocaris_Dash` | 2 | 0,3 s | Se lanza en línea recta | Masa enorme que corta el agua, "fwOOM" |
| `SFX_Boss_Anomalocaris_Swipe` | 2 | 0,25 s | Zarpazo (`Effect_Anomalocaris_Swipe`) | Apéndices que se cierran: tajo con crujido |
| `SFX_Boss_Anomalocaris_Circle` | 1 | 3,0 s | Cerco: nada en espiral alrededor del jugador (3 s) | Remolino que se cierra y sube de tono |
| `SFX_Boss_Anomalocaris_LightsOut` | 1 | 0,4 s | Al 25 % se apagan las luces (0,4 s) | Las plantas luminosas se apagan: barrido que se hunde |
| `SFX_Boss_Anomalocaris_Eyes` | 1 | 2,0 s | Escena: solo sus ojos cruzan la sala a oscuras (2 s) | Algo enorme pasa en la oscuridad: agua desplazada lenta y un zumbido grave |
| `SFX_Boss_Anomalocaris_Roar` | 1 | 0,6 s | Ruge antes del frenesí (0,6 s) | Rugido acuático y burbujeante, con chasquidos |
| `SFX_Boss_Anomalocaris_Charge` | 1 | 0,8 s | Frenesí: quieto, cargando, con el dash marcado en el suelo (0,8 → 0,5 s) | Carga que sube rápido de tono; el juego la corta al salir |
| `SFX_Boss_Anomalocaris_Frenzy` | 2 | 0,3 s | Frenesí: dash larguísimo por la línea | Como el dash, más agudo y violento |
| `SFX_Boss_Anomalocaris_Leap` | 1 | 0,25 s | Salta a morder al final del dash (0,22 s) | Impulso y dentellada |
| `SFX_Boss_Anomalocaris_Stun` | 1 | 0,5 s | Aturdido tras el salto (0,5 s, el momento de pegarle) | Golpe sordo y apéndices que tiemblan |
| `SFX_Boss_Anomalocaris_Silhouette` | 1 | 3,0 s | Silueta de ojos brillantes, invulnerable, rondando (3 s) | Zumbido grave y oscilante, amenaza que da vueltas |
| `SFX_Boss_Anomalocaris_Hurt` | 3 | 0,12 s | Recibe daño | Golpe sobre caparazón blando, grave |
| `SFX_Boss_Anomalocaris_Death` | 1 | 2,5 s | Muere | Se desploma con un último chasquido y burbujas que suben |

### Jefe: Euriptérido y la cueva

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Eurypterid_Step` | 3 | 0,1 s | Pasos al andar (cada pocos pasos; muy bajo) | Patas acorazadas sobre roca: "tok" grave |
| `SFX_Boss_Eurypterid_PincerWarn` | 1 | 0,6 s | Abre las pinzas y se echa atrás (0,6 s; en la fase blanda, 0,45 s) | Pinzas que se abren con chasquido y tensión que sube; el juego la corta en la fase blanda |
| `SFX_Boss_Eurypterid_Pincer` | 2 | 0,2 s | Cierra las pinzas | Tijeretazo de quitina, contundente |
| `SFX_Boss_Eurypterid_ArmorBlock` | 2 | 0,1 s | Golpe de frente: la coraza para la mitad | Golpe sordo contra coraza, "tunk" |
| `SFX_Boss_Eurypterid_SpineWarn` | 1 | 0,6 s | Eriza las púas y las carga de veneno (0,6 s) | Púas que se erizan con un siseo venenoso que crece |
| `SFX_Boss_Eurypterid_SpineShot` | 1 | 0,3 s | Suelta el abanico de púas | Tres púas que silban a la vez |
| `SFX_Boss_Eurypterid_Stunned` | 1 | 0,8 s | Le cae una piedra del techo y queda aturdido | Golpe de roca sobre coraza y un gruñido |
| `SFX_Boss_Eurypterid_Molt` | 1 | 3,0 s | Escena de la muda (3 s) | La coraza cruje, se abre y se desprende; sale blando |
| `SFX_Boss_Eurypterid_TailWarn` | 1 | 0,6 s | Fase blanda: lleva la cola a un lado y la punta brilla en verde (0,6 s) | Cola que se tensa y siseo verde |
| `SFX_Boss_Eurypterid_TailSweep` | 1 | 0,55 s | Barrido de cola de 180° con estela venenosa (0,55 s) | Barrido pesado que silba en arco |
| `SFX_Boss_Eurypterid_Downed` | 1 | 4,5 s | Último aliento: va tambaleándose al centro (2,5 s), cae rendido y jadea (2 s) | Pasos torpes, un derrumbe y un jadeo lento |
| `SFX_Boss_Eurypterid_Rise` | 1 | 1,0 s | Se levanta rugiendo | Rugido que crece hasta la primera salva |
| `SFX_Boss_Eurypterid_FuryShot` | 3 | 0,06 s | Cada salva del bullet hell de 25 s | Pop grave muy corto; muy bajo, suena sin parar |
| `SFX_Boss_Eurypterid_Hurt` | 3 | 0,12 s | Recibe daño | Golpe en coraza, grave y seco (en la fase blanda, el juego baja el tono) |
| `SFX_Boss_Eurypterid_Death` | 1 | 2,5 s | Muere | Se desploma con estrépito de coraza y burbujas |
| `SFX_Cave_Creak` | 2 | 1,5 s | La cueva cruje (`CaveCollapse`: Creaking) | Crujido de roca profundo y lejano |
| `SFX_Cave_Quake` | 1 | 3,0 s | Terremoto (`CaveCollapse`: Quake, 3 s) | Rumor grave que tiembla, con piedras sueltas |
| `SFX_Cave_Collapse` | 1 | 1,0 s | Derrumbe (`CaveCollapse`: Collapse) | Golpe enorme de rocas que caen |
| `SFX_Cave_RockFall` | 2 | 0,5 s | Una roca cae del techo y golpea el suelo | Silbido corto y golpe de piedra con polvo |
| `SFX_Cave_Pebble` | 3 | 0,06 s | Piedrecitas que caen (muchas por segundo en el terremoto) | Clic de grava diminuto; muy bajo |
| `SFX_Cave_FloorCrack` | 1 | 1,5 s | El suelo se agrieta antes de hundirse (aviso de 1,5 s) | Grieta que avanza crujiendo; es la señal para apartarse |
| `SFX_Cave_FloorSink` | 1 | 0,8 s | El suelo se hunde | Roca que se desploma hacia lo hondo |

### Salas del piso 2

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Room_Slabs_Close` | 1 | 0,5 s | Las puertas de losas de roca se cierran al empezar un combate (en el piso 2 sustituye a la membrana, `Room_DoorsClose`; decidido por el usuario) | Dos losas que chocan, con polvo y burbujas. Anuncia el combate |
| `SFX_Room_Slabs_Open` | 1 | 0,6 s | Se abren al limpiar la sala (sustituye a `Room_DoorsOpen`) | El sello se enciende y las losas se retiran rozando la roca |
| `SFX_Room_WaveStart` | 1 | 0,6 s | Empieza otra oleada (`Room.WaveStarted`: Sala sellada, Puente roto) | Pulso grave de aviso, como un cuerno de caracola lejano |
| `SFX_Room_WaveVent` | 1 | 0,6 s | Una grieta del suelo se abre, brilla en rojo y burbujea al salir un enemigo (`WaveVent`) | Roca que se abre y burbujeo que escapa |
| `SFX_Room_Clam_Open` | 1 | 0,6 s | Se abre una almeja pequeña (ahora suena `Mutation_Bubble_Spawn`) | Concha que se abre con un chasquido húmedo |
| `SFX_Room_Clam_Open_Big` | 1 | 1,0 s | Se abre una almeja grande con su mutación especial | La misma concha, más grande, con un brillo dorado |
| `SFX_Room_HeartCoral` | 1 | 0,8 s | Se recoge el corazón morado del coral (`HeartCoral`; ahora suena `Mutation_Bubble_Spawn`) | Latido profundo y acorde misterioso |
| `SFX_Player_PurpleHeart_Block` | 1 | 0,3 s | El corazón morado para un golpe entero | Latido que se rompe con un brillo morado |
| `SFX_Player_PitFall` | 1 | 0,8 s | El jugador cae a una fosa (−½ corazón y vuelve al último suelo firme) | Resbalón, silbido que baja y "bloop" amortiguado |
| `SFX_Exchange_Capsule_Open` | 1 | 0,6 s | Se abre la ventana de la cápsula (ahora suena `Mutation_ChoiceOpen`) | Cápsula que se ilumina con un burbujeo de laboratorio |
| `SFX_Exchange_Capsule_Accept` | 1 | 1,2 s | Se acepta el cambio y la cápsula transforma la mutación (ahora suena `Mutation_Gain` y `Room_Exit`) | Burbujeo que crece, zumbido de cristal y un acorde que se resuelve |

## Se reutilizan del piso 1

Jugador base, interfaz, biomasa, subidas de nivel, elección de mutación, beneficios de linaje comunes (Rematar,
Pastar, Toxina), estados (veneno, aturdido, sangrado, ceguera), sala limpia, salto y caída de la sala de paso,
metamorfosis, tienda y sus fragmentos.

## Comprobaciones antes de entregar

Las del piso 1, y además:
- Las capas de cada franja duran exactamente lo mismo en muestras (`lint_audio.py` lo comprueba).
- `Music_Boss_Eurypterid_LastStand.ogg` dura 25,0 s, y sus tramos cambian a los 8 y a los 16 s.
- Los avisos (Trilobite, Ottoia, Anomalocaris, Euriptérido, grietas) duran lo que el aviso del código y suben
  hacia el final.
- Los sonidos frecuentes nuevos (`Cnidocytes_Shot`, `JawPlates_Bite`, `LarvaBite`, `Anomalocaris_Bubble`,
  `Eurypterid_FuryShot`, `Eurypterid_Step`, `Cave_Pebble`, `Enemy_*_Hit`) no cansan a su ritmo real.
- Toda la música del piso 2 junta no pasa de unos 12 MB.
