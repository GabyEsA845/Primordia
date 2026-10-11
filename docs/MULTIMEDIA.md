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
| Proyectil base del jugador | Disparo sin mutación de Boca | M | Pendiente |
| Proyectil: Mandíbulas | Corto y potente | M | Pendiente |
| Proyectil: Aguijón | Envenena | M | Pendiente |
| Proyectil: Espora | Lento, en abanico de 3 | M | Pendiente |
| Proyectil enemigo | Lento, de color fuerte y fácil de leer | M | Pendiente |
| Proyectil del jefe | Variante del enemigo para la espiral | M | Pendiente |
| Impacto | Al golpear a algo | M | Pendiente |
| Muerte de enemigo | Estallido o burbujas | M | Pendiente |
| Estela de esquiva | Rastro mientras el jugador es invulnerable | D | Pendiente |
| Veneno | Indicador de daño en el tiempo sobre el enemigo | D | Pendiente |
| Nube de tinta | Habilidad Vejiga de tinta | M | Pendiente |
| Onda de choque | Habilidad Glándula de choque | M | Pendiente |

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
