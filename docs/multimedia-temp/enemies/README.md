# Sprites de enemigos y jefe (piso 1)

Pixel art a 16 píxeles por unidad, mismo estilo que el jugador. Cada carpeta tiene las hojas (una tira horizontal por animación), `frames/` con los cuadros sueltos, `gif/` y una `vista_previa_*.png` solo para revisar.

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*. *Slice* → *Grid By Cell Size* con el tamaño de la tabla y pivote *Center*. Todos miran a la derecha; para la izquierda, *Flip X*.

| Enemigo | Animación | Cuadros | Celda | Cuándo |
|---------|-----------|---------|-------|--------|
| Bacteria | Move | 4 | 16×16 | Persigue (mueve el flagelo) |
| | Hurt | 2 | 16×16 | Recibe daño |
| | Death | 5 | 24×24 | Muere (revienta en gotas) |
| Ameba | Move | 4 | 16×16 | Se mueve cambiando de forma |
| | Shoot | 4 | 24×24 | Saca un seudópodo y dispara (el proyectil sale en el cuadro 3) |
| | Hurt | 2 | 16×16 | Recibe daño |
| | Split | 5 | 24×24 | Se divide al morir (en el último cuadro, crear las 2 amebas pequeñas) |
| | Death | 5 | 24×24 | Muerte sin división, por si la usas |
| AmebaSmall | Move | 4 | 10×10 | Se mueve |
| | Hurt | 2 | 10×10 | Recibe daño |
| | Death | 5 | 16×16 | Muere |
| BossVirus | Idle_P1 | 4 | 48×48 | Fase 1: gira mientras dispara la espiral (bucle) |
| | Burst_P1 | 5 | 48×48 | Ráfaga circular: se contrae, brilla y dispara en el cuadro 3 |
| | Hurt_P1 | 2 | 48×48 | Daño en fase 1 |
| | Phase2_Transition | 4 | 48×48 | Al bajar del 50 %: tiembla, se agrieta y se pone rojo |
| | Idle_P2 | 4 | 48×48 | Fase 2: espiral de dos brazos, gira más rápido (bucle) |
| | Burst_P2 | 5 | 48×48 | Ráfaga de 16 en fase 2 |
| | Summon_P2 | 4 | 48×48 | Abre la boca para invocar bacterias (aparecen en el cuadro 2) |
| | Hurt_P2 | 2 | 48×48 | Daño en fase 2 |
| | Death | 10 | 64×64 | Se infla, pierde espículas y estalla |

El jefe mide 3×3 unidades (48 px), como en el GDD. `generar_enemigos.py` vuelve a generar todo: `python3 generar_enemigos.py <carpeta>` (necesita Pillow).
