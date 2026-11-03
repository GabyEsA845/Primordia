# Encargo: música y efectos de sonido de Primordia (piso 1)

Necesito **toda la música y los efectos de sonido** del piso 1 de mi juego, *Primordia*: un roguelike de disparos con vista cenital (twin-stick, como *Enter the Gungeon*) en pixel art. El jugador es una **célula verde** en un **caldo primordial bajo el agua** que se come lo que derrota y muta su cuerpo (Boca, Extremidades, Piel y Órgano especial). Se publica en **itch.io como juego web (WebGL)**.

Hazlo con un script de Python, `generar_audio.py`, que **sintetice todos los sonidos por código** (osciladores, ruido, envolventes, filtros, retardo y reverberación sencillos) con `numpy` (instálalo con pip si falta) y `wave` de la biblioteca estándar. Python 3.12 está en `%LOCALAPPDATA%/Programs/Python/Python312/python.exe` (el `python` del PATH es el alias de la Microsoft Store: usa la ruta completa). Para pasar la música a `.ogg`, usa el `ffmpeg` que trae el paquete `imageio-ffmpeg`, ya instalado (`imageio_ffmpeg.get_ffmpeg_exe()`). Todo con semilla fija, para que al volver a ejecutar el script salga lo mismo. Los parámetros de cada sonido, agrupados por categoría, para poder retocar uno sin tocar los demás.

## Estilo sonoro

- **Chiptune de 16 bits orgánico y acuático.** Que suene a consola de la época de la SNES, como el pixel art, pero húmedo y vivo: burbujas, chapoteos, membranas que se estiran, gotas, gelatina que se aplasta. Nada de metal, pólvora ni armas de fuego: aquí todo es biología.
- **Legible y corto** (pilar 3 del juego: "acción legible y rápida"). Cada sonido se reconoce en una fracción de segundo y no tapa a los demás. Los sonidos que se repiten mucho (disparos, golpes, biomasa) son muy cortos, suaves y con **2 o 3 variantes** para que no cansen.
- **Paleta por familia**, para que se lean sin mirar:
  - **Jugador:** tonos medios y limpios, "verdes": burbujas, chasquidos y pops.
  - **Enemigos:** más graves y viscosos, algo disonantes.
  - **Jefes:** enormes, graves y con cuerpo; el virus suena eléctrico y cristalino (cápside), la ameba suena a gelatina gigante.
  - **Veneno y hongos:** siseos, efervescencia, polvo que sale (esporas como un "puf" seco).
  - **Interfaz:** pequeños "blips" de burbuja, agudos y secos, siempre en la misma tonalidad.
- **Tonalidad común:** todo lo melódico (interfaz, jingles, mejoras, música) en **re menor / re dórico**, para que los avisos y la música no choquen.

## Formato y entrega

| Tipo | Formato | Notas |
|------|---------|-------|
| Efectos de sonido | `.wav` PCM 16 bits, 44 100 Hz, **mono** | Pico máximo −1 dBFS. Sin silencio al principio (el ataque empieza en la primera muestra) y con un fundido de 5 ms al final para que no chasquee |
| Bucles de efecto (`_Loop`) | `.wav` como los anteriores | El final enlaza con el principio sin chasquido (cruce en un paso por cero o fundido cruzado) |
| Música | `.ogg` Vorbis, calidad 5 (unos 160 kbps), 44 100 Hz, **estéreo** | Bucles **exactos en compases** (sin silencio al final ni al principio), con la cola de la reverberación del final sumada al principio para que el salto no se note. Sonoridad media parecida en todas las pistas (unos −16 LUFS; con medir RMS basta) |
| Fragmentos (`_Sting`) | `.ogg` como la música, sin bucle | Terminan en silencio |

- Tamaño: el juego va por la web, así que **la música en total no debería pasar de unos 12 MB** y cada efecto de unos 200 KB.
- **Carpeta de salida:** `docs/multimedia-temp/audio/` con `SFX/`, `Music/` y el script en la raíz. Luego los importaré a Unity (`Assets/_Primordia/Audio/SFX` y `Audio/Music`) con un menú del editor.
- **Nombres:** `SFX_<Categoría>_<Nombre>.wav` y `Music_<Nombre>.ogg`, exactamente como en las tablas. Las variantes llevan `_01`, `_02`, `_03`.
- **Muestrario:** genera también `muestrario.html`, una página con todos los sonidos agrupados por categoría, cada uno con su reproductor (`<audio controls>`), su nombre, su duración y la descripción de la tabla. Así puedo revisarlos rápido en el navegador.
- **Volumen relativo:** en el muestrario, junto a cada sonido, propón el volumen al que debería sonar en el juego (de 0 a 1). Lo usaré al montar el mezclador. Grupos del mezclador: `Music`, `SFX`, `UI`.

## Música

El juego cambia de pista según el estado (`GameState`: menú, jugando, pausa, elección de mutación, victoria y derrota) y según la sala. **Las dos capas del piso 1 tienen el mismo tempo, la misma tonalidad y la misma duración exacta**: el juego las reproduce a la vez y funde una con otra al empezar y al terminar un combate (puertas cerradas ↔ sala limpia).

| Archivo | Uso | Duración | Ambiente |
|---------|-----|----------|----------|
| `Music_MainMenu.ogg` | Menú principal (bucle). El fondo es un caldo con rayos de luz, partículas que suben y siluetas que pasan | 60–90 s | Calmado, misterioso y orgánico: pads cálidos, arpegio lento tipo caja de música acuática, burbujas que suben. Que invite a empezar |
| `Music_Floor1_Explore.ogg` | Piso 1, salas sin enemigos, pasillos y salas ya limpias (bucle, capa A) | 64 compases a 100 BPM (unos 2:34) o la mitad si queda muy grande | Sopa primordial tranquila: bajo redondo, percusión suave de gotas y burbujas, melodía que flota. Curiosidad |
| `Music_Floor1_Combat.ogg` | Piso 1, con las puertas cerradas (bucle, capa B, **en sincronía con la A**) | Igual que la A | La misma armonía con más energía: batería chiptune, bajo más rítmico, arpegios rápidos. Tensión que empuja a moverse |
| `Music_Shop.ogg` | Tienda de Darwin (bucle) | 30–45 s | Simpático y de gabinete de naturalista del siglo XIX bajo el agua: celesta o clavecín burbujeante, vals ligero. Segura y amable |
| `Music_Boss_Virus.ogg` | Jefe: virus gigante (bucle). Dispara en espiral y en ráfagas, invoca bacterias | 60–90 s | Intensa y eléctrica: bajo pulsante, arpegios cristalinos y fríos, percusión dura. Que suene a infección |
| `Music_Boss_Amoeba.ogg` | Jefe: ameba gigante, fases 1 a 3 (bucle). Embiste contra las paredes y se divide | 60–90 s | Pesada y gelatinosa: groove lento y amenazante que se tambalea, bajo que "rebota", golpes graves en las embestidas |
| `Music_Boss_Amoeba_Final.ogg` | Ameba gigante, escena final: 30 s de lluvia de proyectiles en 3 tramos de 10 s. **No es bucle**: dura **exactamente 30 s** y empieza justo al acabar la escena | 30 s | Clímax en 3 tramos que suben de intensidad (anillos → red de brazos → todo a la vez), y corte seco al final, cuando la ameba queda agotada |
| `Music_Sting_LineageEvolution.ogg` | Al completar un linaje: el juego se congela 2 s y la célula se transforma en el animal | 2,0 s | Fanfarria breve y mágica (arpa ascendente, campanas, un acorde mayor luminoso). Sustituye a la música mientras dura, que luego sigue |
| `Music_Sting_BossIntro.ogg` | Al entrar en la sala del jefe, antes de que empiece su música | 2–3 s | Golpe grave y crescendo que corta en seco |
| `Music_Sting_Victory.ogg` | Pantalla de victoria | 5–8 s | Triunfal y orgánica: la célula se ha comido al jefe |
| `Music_Sting_Defeat.ogg` | Pantalla de derrota | 4–6 s | Melancólica y descendente, una burbuja que se apaga; sin dramatismo excesivo, porque se juega muchas veces |

## Ambiente

| Archivo | Uso | Duración | Notas |
|---------|-----|----------|-------|
| `SFX_Amb_Floor1_Loop.wav` | Debajo de la música en todo el piso 1 (bucle, muy bajo) | 20–30 s | Agua en calma: burbujas sueltas que suben, rumor grave, algún chasquido microscópico. Sin ritmo (no debe pelear con la música) |
| `SFX_Amb_BossRoom_Loop.wav` | Sala del jefe antes y después del combate (bucle) | 15–20 s | Igual pero más denso y oscuro, con un zumbido de infección |
| `SFX_Amb_Floor2_Loop.wav` | Océano cámbrico: sube al acercarse a la fosa de la sala de paso y será el ambiente del piso 2 (bucle) | 20–30 s | Mar abierto y profundo: rumor muy grave con oleaje lento, burbujas grandes y lentas, corrientes lejanas y un fondo grave que respira. Sin ritmo |

## Efectos de sonido

En cada tabla, la columna "Cuándo suena" dice qué lo dispara en el juego (entre paréntesis, el evento del código, para que luego sea fácil conectarlo). Duraciones orientativas.

### Jugador

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Player_Shot` | 3 | 0,08 s | Disparo base sin Boca, 5 por segundo (`Weapon.Fired`) | Burbujita que sale disparada: "plip" corto y agudo. Muy suave: suena sin parar |
| `SFX_Player_Dodge` | 2 | 0,25 s | Esquiva con Espacio (`PlayerController.DodgeStarted`) | Impulso líquido rápido: "fssh" de agua desplazada con una pequeña subida de tono |
| `SFX_Player_DodgeReady` | 1 | 0,1 s | La esquiva se recarga al instante por el toque del Tiburón (Sangre en el agua) | "Ding" líquido brillante, discreto |
| `SFX_Player_Hurt` | 2 | 0,3 s | El jugador pierde vida (`Health.Damaged` del jugador) | Chapoteo con quejido de membrana, golpe seco y grave. Debe oírse por encima de todo |
| `SFX_Player_Death` | 1 | 1,2 s | El jugador muere (`Health.Died`) | La célula revienta: estallido húmedo y burbujas que se apagan hacia abajo |
| `SFX_Player_Heal` | 1 | 0,5 s | Se cura medio corazón o uno entero (Mandíbulas al matar, Vacuola, Pastar biomasa, curación de la tienda) | Arpegio ascendente corto y cálido con burbujas |
| `SFX_Player_LowHealth_Loop` | 1 | 1 s | Bucle con medio corazón o menos (opcional para el juego) | Latido suave y grave, muy bajo |
| `SFX_Player_AbilityReady` | 1 | 0,15 s | La barra de recarga del Órgano se llena y destella (`CooldownBar`) | "Tic" brillante y limpio |
| `SFX_Player_AbilityNotReady` | 1 | 0,1 s | Clic derecho con el Órgano aún recargando | "Tuc" apagado y sordo, muy bajo |

### Mutaciones: Boca (ataque principal)

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Mouth_Mandibles_Bite` | 3 | 0,15 s | Mordisco de Mandíbulas, 2,5 por segundo (`Weapon.Fired`) | Chasquido de mandíbulas que se cierran: "clac" húmedo y carnoso |
| `SFX_Mouth_Mandibles_BiteHit` | 2 | 0,15 s | El mordisco alcanza a un enemigo (`Weapon.HitTarget`) | Crujido jugoso encima del chasquido |
| `SFX_Mouth_Mandibles_Lunge` | 1 | 0,12 s | Avance de 0,6 u del nivel II, si nadie está a tiro | Arrancada corta y rápida, "fup" |
| `SFX_Mouth_Stinger_Shot` | 3 | 0,08 s | Disparo del Aguijón, 4 por segundo | Dardo fino: "tsip" agudo con un poco de silbido |
| `SFX_Mouth_Spores_Puff` | 2 | 0,3 s | Bocanada de 5 o 7 esporas, 1,25 por segundo | "Puf" seco y polvoriento de hongo, con el gorrito que se aplasta (algo blando) |
| `SFX_Mouth_Spore_Pop` | 2 | 0,08 s | Una espora toca a un enemigo | Pop diminuto y apagado |

### Mutaciones: Extremidades, Piel y Órgano

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Limbs_Fins_Dodge` | 2 | 0,35 s | Esquiva con Aletas (más larga) en lugar de la base | Aleteo líquido, dos golpes de aleta |
| `SFX_Limbs_Legs_Push` | 2 | 0,2 s | Patas articuladas: la esquiva empuja y aturde a un enemigo | Golpe de patitas quitinosas, "toc-toc" con un empujón |
| `SFX_Limbs_Legs_Stomp` | 1 | 0,4 s | Pisotón (Patas III): onda de radio 2 al acabar la esquiva | Golpe grave al suelo con una ondulación |
| `SFX_Limbs_Fins_Ambush` | 1 | 0,4 s | Emboscada (Aletas III): +50 % de daño 1,5 s tras esquivar, con aura | Brillo que sube con un toque de tensión |
| `SFX_Limbs_Hyphae_Trail` | 3 | 0,15 s | Hifas: cada mancha de micelio, una de cada pocas (el juego elige cuándo; muy bajo) | Crujidito de fibras que brotan |
| `SFX_Skin_Thorns` | 2 | 0,12 s | Piel urticante: daña a quien te toca | Pinchazo eléctrico pequeño |
| `SFX_Skin_Quills` | 1 | 0,4 s | Erizo (Piel urticante III): 8 púas en círculo al recibir un golpe | Ráfaga de púas que salen a la vez, "fshhk" |
| `SFX_Skin_Plates_Block` | 1 | 0,3 s | Placas (Caparazón III) anulan el primer golpe de la sala (`Health.ShieldBlocked`) | Golpe sobre concha, sordo y resistente |
| `SFX_Skin_Plates_Restore` | 1 | 0,3 s | Las Placas se recargan al entrar en otra sala | Concha que encaja, "clic" mineral |
| `SFX_Skin_Camo_On` | 1 | 0,5 s | Camuflaje: el jugador se vuelve casi transparente | Desvanecimiento suave, barrido que baja de volumen y de tono |
| `SFX_Skin_Camo_Off` | 1 | 0,25 s | Deja de estar camuflado (ataca, usa la habilidad, recibe un golpe o lo descubren) | Lo contrario: aparición rápida |
| `SFX_Skin_Camo_Strike` | 1 | 0,2 s | Ataque desde el camuflaje (doble o triple de daño) | Acento afilado que se suma al ataque |
| `SFX_Organ_Ink` | 1 | 1,0 s | Vejiga de tinta: nube que ciega 3 s (`PlayerAbility.Used`) | Chorro espeso que se expande, "blorp" y nube que se dispersa |
| `SFX_Organ_Shockwave` | 1 | 0,6 s | Glándula de choque: onda que destruye proyectiles y empuja | Pulso eléctrico y acuático, "vwuum" con chispa |
| `SFX_Organ_Shockwave_Paralyze` | 1 | 0,5 s | Paralizante (Glándula III): la onda paraliza 2 s | Crepitar eléctrico encima de la onda |
| `SFX_Organ_Shockwave_ProjectileBreak` | 2 | 0,1 s | Cada proyectil enemigo que la onda destruye | Pop cristalino minúsculo |
| `SFX_Organ_Vacuole` | 1 | 0,7 s | Vacuola digestiva: digiere y cura | Glu-glu de digestión y una burbuja satisfecha (el `SFX_Player_Heal` puede sonar encima) |
| `SFX_Organ_Vacuole_Charge` | 1 | 0,1 s | La Vacuola suma una muerte a su carga | Gotita, muy baja |

### Mejoras, linajes y beneficios

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Mutation_Gain` | 1 | 0,6 s | Se equipa una mutación nueva (`PlayerMutations.Changed`) | Transformación orgánica: burbujeo que crece y un acorde menor que se resuelve |
| `SFX_Mutation_LevelUp` | 1 | 0,6 s | Sube a nivel II (`PlayerMutations.LevelChanged`; efecto `Effect_LevelUp`) | Arpegio ascendente brillante |
| `SFX_Mutation_LevelUp_Gold` | 1 | 1,0 s | Sube a nivel III, la evolución (`Effect_LevelUp_Gold`) | El mismo arpegio, más largo, con campanas doradas y un acorde mayor |
| `SFX_Lineage_Complete` | 1 | 0,8 s | Cartel "¡Tiburón! Rematar + …" tras la evolución (`LineageEvolution.Evolved`) | Destello dorado y golpe de acorde. La animación de 2 s usa `Music_Sting_LineageEvolution` |
| `SFX_Lineage_Lost` | 1 | 0,4 s | Se pierde un linaje: pof del color de su clase (`PlayerLineage.ActiveChanged`) | Pof que se desinfla, tono descendente |
| `SFX_Lineage_Finisher` | 2 | 0,15 s | Rematar (carnívoros): golpe con +100 % a un enemigo herido | Acento grave y contundente que se suma al golpe |
| `SFX_Lineage_Barrier` | 1 | 0,4 s | Pastar (herbívoros) da el medio corazón verde | Brote vegetal que se cierra, suave |
| `SFX_Lineage_BarrierBlock` | 1 | 0,3 s | El corazón verde absorbe un golpe (`Health.BarrierBlocked`) | Hoja que se rompe, crujido verde |
| `SFX_Lineage_Colonize` | 1 | 0,6 s | Colonizar (hongos): brotan las matas al entrar en una sala de combate | Varios "puf" de setas que brotan escalonados |
| `SFX_Lineage_Retract` | 1 | 0,3 s | Retraerse (Tortuga): invulnerable 1,5 s al usar la Vacuola | Meterse en el caparazón, "tunk" hueco |
| `SFX_Lineage_StingingTrail` | 1 | 0,4 s | Estela urticante (Medusa) al esquivar | Cosquilleo eléctrico que se arrastra |
| `SFX_Lineage_PuffOnHit` | 1 | 0,5 s | Bocanada al golpe (Cuesco de lobo): nube cegadora al recibir un golpe | "Puf" grande y polvoriento |

### Estados de los enemigos

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Status_Poison` | 2 | 0,3 s | Se aplica veneno (Aguijón, Piel urticante II, nubes de esporas) | Siseo efervescente, ácido |
| `SFX_Status_PoisonBurst` | 1 | 0,4 s | Contagio (Aguijón III): un envenenado muere y estalla | Estallido verde, burbujeo que se reparte |
| `SFX_Status_Bleed` | 1 | 0,25 s | Desgarro (Mandíbulas III) hace sangrar | Corte húmedo y breve |
| `SFX_Status_Blind` | 1 | 0,3 s | Un enemigo queda cegado por la tinta | Gorgoteo confuso |
| `SFX_Status_Stun` | 1 | 0,3 s | Aturdido (Patas, Pisotón) | Pajaritos chiptune: trino corto que gira |
| `SFX_Status_SporeCloud` | 1 | 0,5 s | Red micelial (Hifas III): un enemigo muere sobre el micelio y suelta esporas | Nube de esporas que se abre |
| `SFX_Status_Suspicion` | 1 | 0,3 s | Camuflaje: un enemigo sospecha ("?" sobre la cabeza) | Dos notas interrogantes, subida pequeña |
| `SFX_Status_Discovered` | 1 | 0,25 s | Te descubren ("!") | Nota de alarma corta, aguda |
| `SFX_Status_Panic` | 1 | 0,5 s | Pánico (Camuflaje III): los enemigos se asustan y se alejan 2 s | Gemido tembloroso que baja |

### Enemigos del piso 1

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Enemy_Hit` | 3 | 0,1 s | Un enemigo recibe daño (`Health.Damaged`) | "Plop" viscoso corto; distinto del daño del jugador |
| `SFX_Enemy_Death` | 3 | 0,35 s | Un enemigo muere, con burbujas (`Enemy.Died`, `Effect_EnemyDeath`) | Reventón gelatinoso y burbujas que suben |
| `SFX_Enemy_Bacteria_Aggro` | 2 | 0,3 s | Una bacteria (perseguidor) ve al jugador y empieza a perseguirlo | Chirrido viscoso, agresivo y pequeño |
| `SFX_Enemy_Amoeba_Windup` | 1 | 0,4 s | La ameba (tirador) avisa antes de disparar (`AimStarted`) | Se hincha: gorgoteo que sube de tono. Aviso claro para esquivar |
| `SFX_Enemy_Amoeba_Shot` | 2 | 0,15 s | La ameba dispara | "Ptoo" de gota lanzada, grave |
| `SFX_Enemy_Amoeba_Split` | 1 | 0,5 s | La ameba se divide al morir en dos pequeñas (`Enemy.ChildSpawned`) | Estiramiento de gelatina y "plop-plop" de dos gotas |
| `SFX_Enemy_SmallAmoeba_Death` | 2 | 0,2 s | Muere una ameba pequeña | Versión aguda y corta de la muerte |
| `SFX_Projectile_Impact` | 3 | 0,08 s | Un proyectil choca con una pared | Salpicadura diminuta |

### Jefe: virus gigante

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Virus_Intro` | 1 | 1,5 s | Aparece al entrar el jugador (espera 1,5 s antes de atacar) | Zumbido eléctrico que se forma y cristales que encajan |
| `SFX_Boss_Virus_Spiral` | 3 | 0,06 s | Cada proyectil de la espiral (salen muy seguidos) | Tic eléctrico minúsculo. Muy bajo: suena decenas de veces por segundo |
| `SFX_Boss_Virus_BurstWindup` | 1 | 0,6 s | Aviso de la ráfaga circular (`BurstStarting`) | Carga eléctrica que sube |
| `SFX_Boss_Virus_Burst` | 1 | 0,5 s | Salen los 12 o 16 orbes | Descarga circular, golpe con eco |
| `SFX_Boss_Virus_SummonWindup` | 1 | 0,6 s | Aviso de la invocación de bacterias (`SummonStarting`) | Burbujeo infeccioso que crece |
| `SFX_Boss_Virus_Summon` | 1 | 0,4 s | Aparecen 2 bacterias | Dos "plop" de nacimiento |
| `SFX_Boss_Virus_Phase2` | 1 | 1,5 s | Cambio a la fase 2 al 50 % (`PhaseChanged`) | Distorsión que se rompe y sube: rugido eléctrico. Cabe un temblor de cámara |
| `SFX_Boss_Virus_Hurt` | 3 | 0,12 s | Recibe daño | Golpe sobre cápside, cristalino y grave |
| `SFX_Boss_Virus_Death` | 1 | 2,5 s | Muere | Cortocircuito largo, se resquebraja y estalla en burbujas |

### Jefe: ameba gigante

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Boss_Amoeba_Intro` | 1 | 1,5 s | Al entrar en su sala redonda | Masa enorme que se asienta, gorgoteo grave |
| `SFX_Boss_Amoeba_Warning` | 1 | 0,6 s | Aviso de 0,6 s antes de embestir: tiembla, mira hacia donde irá y se encoge | Temblor gelatinoso que se tensa y se comprime, con subida de tono al final. Es la señal para esquivar |
| `SFX_Boss_Amoeba_Launch` | 2 | 0,3 s | Sale disparada | Arrancada líquida potente, "fwoomp" |
| `SFX_Boss_Amoeba_WallSlam` | 3 | 0,5 s | Choca contra una pared y se aplasta | Golpe sordo enorme y chapoteo, "SPLOTCH" grave |
| `SFX_Boss_Amoeba_Enrage` | 1 | 1,2 s | Animación de enfado al 80 % (`StageChanged` a Enraging) | Gruñido gelatinoso que hierve |
| `SFX_Boss_Amoeba_Split` | 1 | 1,0 s | Se parte en dos medianas al 50 % | Desgarro lento de gelatina y dos masas que se separan |
| `SFX_Boss_Amoeba_Bounce` | 2 | 0,25 s | Las dos medianas chocan entre sí y rebotan | "Boing" gelatinoso |
| `SFX_Boss_Amoeba_Merge` | 1 | 3,0 s | Escena final: las medianas se juntan en el centro (3 s) | Dos masas que se acercan y se funden, sonido que crece |
| `SFX_Boss_Amoeba_Fury` | 1 | 2,0 s | Escena final: se enfurece (2 s) antes de la lluvia | Rugido grande y burbujeante que acaba en un golpe, justo antes de `Music_Boss_Amoeba_Final` |
| `SFX_Boss_Amoeba_Barrage` | 3 | 0,06 s | Cada salva de la lluvia de 30 s (anillos, brazos, abanicos) | Pop grave muy corto; muy bajo, suena sin parar |
| `SFX_Boss_Amoeba_Immune` | 2 | 0,1 s | Golpe mientras es inmune (barra gris con candado) | Rebote sordo, "tunk", que diga "no hace daño" |
| `SFX_Boss_Amoeba_Exhausted_Loop` | 1 | 2 s | Agotada en el centro (pálida, barra roja que palpita) | Jadeo lento y blando, burbujas débiles |
| `SFX_Boss_Amoeba_Hurt` | 3 | 0,12 s | Recibe daño | Golpe en gelatina, grave y elástico |
| `SFX_Boss_Amoeba_Death` | 1 | 2,5 s | Cae de dos golpes al final | Se deshace: gelatina que se derrumba y una gran burbuja que revienta |
| `SFX_Cinematic_Bars` | 1 | 0,6 s | Entran las franjas de cine de la escena (`CinematicOverlay`) | Barrido grave y suave |

### Salas, mapa y recompensas

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Room_DoorsClose` | 1 | 0,5 s | Al entrar en una sala de combate, las membranas se cierran (`Room.Entered`) | Membrana que se tensa y se sella, "fwup" grave. Anuncia el combate |
| `SFX_Room_DoorsOpen` | 1 | 0,5 s | Al limpiar la sala, las membranas se abren | La misma al revés, más suave y aliviada |
| `SFX_Room_Cleared` | 1 | 0,8 s | Sala limpia (`Room.Cleared`), junto con la apertura | Mini jingle positivo de 3 notas |
| `SFX_Room_Exit` | 1 | 0,8 s | Se abre la salida tras el jefe (`RoomExit`) | Remolino que se abre |
| `SFX_Transition_Jump` | 1 | 0,45 s | El jugador salta a la fosa de la sala de paso (`TransitionPit`) | Impulso corto y silbido que sube |
| `SFX_Transition_Fall` | 1 | 2 s | Cae por la fosa hacia el piso siguiente | Silbido que baja hacia lo hondo y un "bloop" grave al llegar al fondo, con burbujas |
| `SFX_Biomass_Pickup_Small` | 3 | 0,1 s | Recoger biomasa pequeña (vale 1) (`PlayerBiomass` / `BiomassPickup.Collected`) | "Blip" jugoso y agudo. El juego subirá el tono si se recogen varias seguidas |
| `SFX_Biomass_Pickup_Big` | 1 | 0,25 s | Recoger biomasa grande (vale 5) | Versión más llena con brillo |
| `SFX_Biomass_Magnet` | 1 | 0,6 s | Al limpiar la sala, toda la biomasa vuela hacia el jugador | Succión que sube de tono |
| `SFX_Mutation_ChoiceOpen` | 1 | 0,5 s | Aparece "elige 1 de 3" (estado `MutationChoice`) | Tres burbujas que suben y se abren |
| `SFX_Mutation_Bubble_Spawn` | 1 | 0,4 s | El jefe suelta las 3 burbujas de mutación | Burbujas que nacen y flotan |
| `SFX_Mutation_Bubble_Pickup` | 1 | 0,4 s | Se coge una burbuja (`MutationPickup.PickedUp`) | Pop grande y limpio (luego suena `SFX_Mutation_Gain`) |
| `SFX_Mutation_Bubble_Burst` | 1 | 0,3 s | Las otras dos revientan | Dos pops pequeños, uno tras otro |

### Tienda

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_Shop_Approach` | 1 | 0,2 s | El jugador se acerca a un pedestal (brillo y tecla E) | Destello suave y agudo |
| `SFX_Shop_Purchase` | 1 | 0,6 s | Compra con E (`ShopPedestal.Purchased`, `Effect_ShopPurchase`) | Caja registradora orgánica: tintineo de conchas y burbujas doradas |
| `SFX_Shop_CantAfford` | 1 | 0,25 s | No llega la biomasa o no sirve (vida llena): el precio se pone rojo y el artículo tiembla | Zumbido corto y grave, dos notas que bajan. Nada molesto |
| `SFX_Shop_Darwin_Cheer` | 2 | 0,6 s | Darwin celebra la compra (animación Buy) | Murmullo de viejecito contento en chiptune (sin palabras reales, como los "bla" de un juego de 16 bits) y burbujas de la barba |
| `SFX_Shop_HeartContainer` | 1 | 0,8 s | Compra del corazón extra (un corazón más de vida máxima) | Latido fuerte y acorde que sube; más valioso que la curación |

### Interfaz

Siempre en re menor, secos y cortos. Suenan también con el juego en pausa.

| Archivo | Var. | Duración | Cuándo suena | Descripción |
|---------|------|----------|--------------|-------------|
| `SFX_UI_Hover` | 1 | 0,05 s | El ratón pasa por un botón, una tarjeta o un recuadro de ranura (`MenuSelector`) | "Tic" de burbuja diminuto |
| `SFX_UI_Click` | 1 | 0,1 s | Pulsar un botón (`MenuButtonFeedback`, burbuja al pulsar) | Pop de burbuja con un poco más de cuerpo |
| `SFX_UI_Back` | 1 | 0,1 s | Volver, cerrar o Saltar en la elección | El pop al revés, más grave |
| `SFX_UI_CardSelect` | 1 | 0,4 s | Elegir una de las 3 tarjetas de mutación | Confirmación más rica: pop y una nota que sube |
| `SFX_UI_StartGame` | 1 | 1,0 s | "Jugar" en el menú principal | Inmersión: burbujas que bajan y un golpe suave |
| `SFX_UI_PauseOpen` | 1 | 0,25 s | Abre la pausa (Esc o P) | Sonido que se ahoga, como meter la cabeza bajo el agua (la música se filtrará en el juego) |
| `SFX_UI_PauseClose` | 1 | 0,25 s | Cierra la pausa | Lo contrario |
| `SFX_UI_MapOpen` | 1 | 0,3 s | Abre el mapa grande (TAB) | Lente que enfoca, cristal |
| `SFX_UI_MapClose` | 1 | 0,2 s | Cierra el mapa | El mismo, más corto y al revés |
| `SFX_UI_OrganReady` | 1 | 0,25 s | El tubo del recuadro del Órgano (HUD, abajo a la izquierda) se llena y destella: la habilidad está lista | "Bloop" de burbuja que sube de tono y un tintineo cristalino encima, como la savia llegando arriba del tubo; en re |

## Más adelante (no lo hagas ahora)

Para que la lista sirva de guía, esto vendrá con los pisos siguientes y **no hay que generarlo en este encargo**: la transición de piso y la metamorfosis del jugador según su familia de arma; la música, el ambiente, los enemigos y los jefes del océano cámbrico (piso 2: trilobites, medusas, Anomalocaris, euriptérido) y del pantano carbonífero (piso 3: libélulas, anfibios, Arthropleura, Meganeura), y los sonidos de sus 8 mutaciones nuevas.

## Comprobaciones antes de entregar

- Están todos los archivos de las tablas (con sus variantes), con el nombre exacto, y ninguno más.
- Ningún efecto satura (pico ≤ −1 dBFS) ni empieza con silencio; los `_Loop` y la música enlazan sin chasquido (escucha el empalme repitiendo el archivo dos veces seguidas en el muestrario).
- Las dos capas del piso 1 duran exactamente lo mismo en muestras.
- `Music_Boss_Amoeba_Final.ogg` dura 30,0 s.
- Los sonidos frecuentes (`Player_Shot`, `Mouth_*_Shot`, `Boss_*_Spiral`, `Boss_Amoeba_Barrage`, `Biomass_Pickup_Small`, `Enemy_Hit`) no cansan al oírlos 20 veces seguidas: añade al muestrario un botón que los repita a su ritmo real.
- Escribe al final un resumen con el número de archivos y el tamaño total de `SFX/` y `Music/`.
