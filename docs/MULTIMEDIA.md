# Primordia: lista de contenido multimedia

Lista viva de sprites, música, efectos y fuentes que necesita el juego. Se actualiza cada vez que se añade, cambia o termina un recurso. El diseño está en [el GDD](GDD%20Primordia.md) y el orden de trabajo en [PLAN.md](PLAN.md).

## Cómo usar esta lista

**Prioridad:**
- **M**: necesario para la primera versión publicada (v1.0).
- **D**: deseable para la v1.0 si da tiempo.
- **S**: más adelante.

**Estado:** `Pendiente` → `Placeholder` (forma o sonido provisional) → `En curso` → `Listo`.

## Especificaciones técnicas

| Tipo | Formato | Ajustes en Unity | Carpeta |
|------|---------|------------------|---------|
| Sprites | `.png` (fuente en `.aseprite`) | 16 píxeles por unidad, filtro *Point*, sin compresión | `Assets/_Primordia/Art/` |
| Personajes | 16×16 a 32×32 px; jefe hasta 64×64 px | Animaciones con Sprite Sheet | `Art/Characters/` |
| Tiles | 16×16 px | Tile Palette + Tilemap | `Art/Tiles/` |
| Música | `.ogg` en bucle | *Streaming*, sin pausas al repetir | `Assets/_Primordia/Audio/Music/` |
| Efectos de sonido | `.wav` cortos | *Decompress On Load* | `Audio/SFX/` |
| Fuentes | `.ttf` / `.otf` pixel | Fuente TextMeshPro sin suavizado | `Art/Fonts/` |

### Carpeta temporal: `docs/multimedia-temp/`

Los recursos nuevos (hojas de sprites, GIF de revisión, scripts que los generan) se dejan primero en `docs/multimedia-temp/<recurso>/`. Esa carpeta **no se sube al repositorio** (está en `.gitignore`); lo que se sube es lo que Unity importa a `Assets/_Primordia/Art/`.

| Recurso | Carpeta temporal | Cómo se importa |
|---------|------------------|-----------------|
| Jugador | `docs/multimedia-temp/player/` (`Player_<Acción>_<dirección>.png`, `Player_Hurt.png`, `Player_Death.png`) | Menú **Primordia > Art > Importar animaciones del jugador**: copia las hojas a `Art/Characters/Player/`, las corta (cuadros del alto de la hoja, pivote centrado), crea los clips y el Animator en `Animations/` y se los pone al prefab del jugador. Se puede repetir cada vez que cambie el arte. |
| Enemigos y jefe | `docs/multimedia-temp/enemies/<Enemigo>/` (`Bacteria`, `Ameba`, `AmebaSmall`, `BossVirus`; hojas `<Enemigo>_<Animación>.png`) | Menú **Primordia > Art > Importar animaciones de enemigos y jefe**: igual que el jugador, en `Art/Characters/Enemies/<Enemigo>/`. Aplica cada uno a su prefab (`Enemy_Chaser`, `Enemy_Shooter`, `Enemy_Small`, `Boss_Virus`) con escala 1, y ajusta los tiempos de juego a las animaciones: aviso de disparo de la ameba, aviso de ráfaga e invocación del jefe, pausa de la transición a la fase 2, división al final de Split y tiempo de muerte. |
| Proyectiles | `docs/multimedia-temp/projectiles/` (`Projectile_<Nombre>.png`, el mismo nombre que su prefab) | Menú **Primordia > Art > Importar animaciones de proyectiles**: copia las hojas a `Art/Projectiles/`, crea un clip y un controlador por proyectil (se reproduce solo al salir del pool) y los aplica a los prefabs de `Prefabs/Combat` con escala 1, conservando el radio de colisión. Las celdas no cuadradas se indican en `ArtSetup` (`Stinger`: 12×6). Mandíbulas no dispara: `Mandibles_Bite.png` crea `Prefabs/Combat/Effect_Bite` (escala 1, en la boca) y convierte el ataque en mordisco; `Projectile_Mandibles.png` ya no se importa. Las duraciones por cuadro se indican en `ArtSetup`. |
| Efectos | `docs/multimedia-temp/effects/` (cada hoja con su nombre, sin prefijo común) | Menú **Primordia > Art > Importar efectos**: crea los prefabs de `Prefabs/Effects` (los que no van en bucle se destruyen solos al acabar, con `OneShotEffect`) y los conecta: impactos en los prefabs de proyectil y en la mordida, veneno en el Aguijón, burbujas en los enemigos, estela en el jugador. La tinta y la onda sustituyen al círculo de `Prefabs/Mutations`; `AbilityMutation.effectRadius` indica a qué radio están dibujadas para no agrandarlas. Necesita haber importado antes enemigos y proyectiles. |

`.png`, `.aseprite`, `.wav`, `.ogg`, `.mp3`, `.ttf` y `.otf` se guardan con Git LFS (ya configurado en `.gitattributes`).

## Sprites

### Jugador (célula, piso 1)

| Recurso | Descripción | Animaciones | Prioridad | Estado |
|---------|-------------|-------------|-----------|--------|
| Célula base | Criatura del piso 1, mira hacia el cursor | Reposo, moverse, esquivar, recibir daño, morir | M | Listo |
| Capa de mutación: Mandíbulas | Visible sobre la célula (pilar "Eres lo que comes") | Reposo, atacar | D | Pendiente |
| Capa de mutación: Aguijón | | Reposo, atacar | D | Pendiente |
| Capa de mutación: Esporas | | Reposo, atacar | D | Pendiente |
| Capa de mutación: Aletas | | Moverse, esquivar | D | Pendiente |
| Capa de mutación: Patas articuladas | | Moverse, esquivar | D | Pendiente |
| Capa de mutación: Caparazón | | Reposo | D | Pendiente |
| Capa de mutación: Piel urticante | | Reposo | D | Pendiente |
| Capa de mutación: Vejiga de tinta | | Activar | D | Pendiente |
| Capa de mutación: Glándula de choque | | Activar | D | Pendiente |

**Piezas de mutación** (encargo completo en `docs/multimedia-temp/mutations/README.md`):

- **Boca** (Mandíbulas, Aguijón, Esporas): `Mouth_<Mutación>_Idle.png` y `_Attack.png`, celdas de 16×16 dibujadas mirando a la derecha. Se colocan en la boca (0,75 unidades del centro) y giran hacia el cursor; detrás de la célula cuando mira hacia arriba.
- **Cuerpo** (Extremidades, Piel, Órgano): `Body_<Mutación>_side/down/up.png`, celdas de 20×20 con la célula centrada y 4 cuadros que siguen el reposo del jugador. Se ocultan al esquivar, recibir daño y morir. Órganos con `_Activate.png` opcional.
- Orden de dibujo, de atrás hacia delante: Extremidades, célula, Órgano, Piel, Boca.

### Enemigos y jefe (piso 1)

| Recurso | Descripción | Animaciones | Prioridad | Estado |
|---------|-------------|-------------|-----------|--------|
| Bacteria | Persigue al jugador | Moverse, recibir daño, morir | M | Listo |
| Ameba | Se divide al morir | Moverse, disparar, dividirse, morir | M | Listo |
| Ameba pequeña | Resultado de la división | Moverse, morir | M | Listo |
| Virus gigante (jefe) | Dispara en espiral | Reposo, atacar, recibir daño, morir | M | Listo |
| Tercer enemigo | Por definir | Por definir | D | Pendiente |

### Proyectiles y efectos visuales

| Recurso | Descripción | Prioridad | Estado |
|---------|-------------|-----------|--------|
| Proyectil base del jugador | Disparo sin mutación de Boca | M | Listo |
| Mordisco: Mandíbulas | Efecto del mordisco cuerpo a cuerpo delante de la boca (`Effect_Bite`, hoja `Mandibles_Bite.png`: 5 cuadros de 24×24 que duran 60, 60, 50, 90 y 90 ms) | M | Listo |
| Proyectil: Aguijón | Envenena | M | Listo |
| Proyectil: Espora | Lento, en abanico de 3 | M | Listo |
| Proyectil enemigo | Lento, de color fuerte y fácil de leer | M | Listo |
| Proyectil del jefe | Mini virión para la espiral y orbe grande para la ráfaga | M | Listo |
| Impacto | Al golpear un objetivo o una pared: lima los disparos y la mordida del jugador (`Effect_Impact_Player`), rosa los enemigos (`Effect_Impact_Enemy`) | M | Listo |
| Muerte de enemigo | Burbujas encima de la muerte (`Effect_EnemyDeath`); no en la división de la ameba ni en el jefe | M | Listo |
| Estela de esquiva | Una silueta cada 0,05 s mientras esquiva (`Effect_DodgeTrail`, componente `DodgeTrail`) | D | Listo |
| Veneno | Burbujas en bucle sobre el enemigo mientras dura (`Effect_Poison`) | D | Listo |
| Nube de tinta | Habilidad Vejiga de tinta; dibujada a radio 3 (96 px) | M | Listo |
| Onda de choque | Habilidad Glándula de choque; dibujada a radio 4 (128 px) | M | Listo |

### Escenario (sopa primordial)

| Recurso | Descripción | Prioridad | Estado |
|---------|-------------|-----------|--------|
| Tileset de suelo | Charco microscópico, tonos verdes | M | Pendiente |
| Tileset de paredes | Bordes de las salas | M | Pendiente |
| Puerta abierta / cerrada | Se cierra durante el combate | M | Pendiente |
| Decoración | Burbujas y partículas de fondo | D | Pendiente |
| Sala del jefe | Variante del tileset o decoración propia | D | Pendiente |

### Objetos e interfaz

| Recurso | Descripción | Prioridad | Estado |
|---------|-------------|-----------|--------|
| Biomasa | Moneda que cae al limpiar una sala | M | Pendiente |
| Mutación menor en el suelo | Objeto para recoger | M | Pendiente |
| Iconos de mutación (×9) | Uno por mutación del GDD | M | Pendiente |
| Corazones | Lleno, medio y vacío | M | Pendiente |
| Mira del ratón | Sustituye al cursor | M | Pendiente |
| Ranuras del cuerpo en el HUD | Boca, Extremidades, Piel, Órgano especial | M | Pendiente |
| Recarga de habilidad | Indicador del clic derecho | M | Pendiente |
| Tarjetas "elige 1 de 3" | Fondo para la elección de mutación | M | Pendiente |
| Logo de *Primordia* | Título del menú principal | M | Pendiente |
| Fondo del menú principal | | D | Pendiente |
| Pantallas de victoria y derrota | | M | Pendiente |
| Iconos de linaje | Depredador, Acorazado, Tóxico | D | Pendiente |
| Portada para la página de itch.io | 630×500 px, captura o arte promocional | M | Pendiente |

## Música

| Pista | Uso | Ambiente | Prioridad | Estado |
|-------|-----|----------|-----------|--------|
| Menú principal | Bucle | Calmado y orgánico | D | Pendiente |
| Piso 1: sopa primordial | Bucle en salas | Acuático, burbujeante, con ritmo | D | Pendiente |
| Jefe: virus gigante | Bucle en la sala del jefe | Intenso | D | Pendiente |
| Victoria | Fragmento corto | | D | Pendiente |
| Derrota | Fragmento corto | | D | Pendiente |

## Efectos de sonido

| Efecto | Cuándo suena | Prioridad | Estado |
|--------|-------------|-----------|--------|
| Disparo base | Clic izquierdo sin mutación | D | Pendiente |
| Disparo: Mandíbulas | Mordida | D | Pendiente |
| Disparo: Aguijón | | D | Pendiente |
| Disparo: Esporas | | D | Pendiente |
| Esquiva | Espacio | D | Pendiente |
| Jugador recibe daño | | D | Pendiente |
| Jugador muere | | D | Pendiente |
| Enemigo recibe daño | | D | Pendiente |
| Enemigo muere | | D | Pendiente |
| Ameba se divide | | D | Pendiente |
| Disparo enemigo | | D | Pendiente |
| Ataque en espiral del jefe | | D | Pendiente |
| Puertas se cierran / se abren | Al entrar y al limpiar una sala | D | Pendiente |
| Recoger biomasa | | D | Pendiente |
| Obtener mutación | | D | Pendiente |
| Habilidad: Vejiga de tinta | Clic derecho | D | Pendiente |
| Habilidad: Glándula de choque | Clic derecho | D | Pendiente |
| Interfaz: navegar y confirmar | Menús | D | Pendiente |

## Fuentes

| Recurso | Uso | Prioridad | Estado |
|---------|-----|-----------|--------|
| Fuente pixel | HUD, menús y textos | M | Pendiente |

## Registro de licencias

Anotar aquí todo recurso que no sea propio (bancos de sonido, fuentes, etc.) para respetar sus licencias y dar créditos en la página de itch.io.

| Recurso | Autor / fuente | Licencia | Enlace |
|---------|---------------|----------|--------|
| | | | |
