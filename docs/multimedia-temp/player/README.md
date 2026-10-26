# Sprites del jugador (célula)

Pixel art a 16 píxeles por unidad. `vista_previa.png` y la carpeta `gif/` son solo para revisar; no hace falta importarlas en Unity.

| Hoja | Cuadros | Tamaño | Uso |
|------|---------|--------|-----|
| `Player_Idle_side/down/up.png` | 4 | 16×16 | Reposo |
| `Player_Move_side/down/up.png` | 4 | 16×16 | Moverse |
| `Player_Dodge_side/down/up.png` | 5 | 16×16 | Esquivar (rueda) |
| `Player_Hurt.png` | 2 | 16×16 | Recibir daño (blanco y rojo) |
| `Player_Death.png` | 7 | 24×24 | Morir (se infla y revienta) |

- **Direcciones:** `side` mira a la derecha; para la izquierda se voltea con *Flip X* del SpriteRenderer. `down` mira a la cámara y `up` está de espaldas.
- **Importar en Unity:** menú *Primordia > Art > Importar animaciones del jugador*. Copia las hojas a `Assets/_Primordia/Art/Characters/Player/`, las configura (Multiple, 16 PPU, Point, sin compresión), las corta con pivote centrado, crea los clips y el Animator y actualiza el prefab del jugador.
- `frames/` tiene los mismos cuadros sueltos por si prefieres arrastrarlos uno a uno.
- `generar_jugador.py` vuelve a generar todo: `python3 generar_jugador.py <carpeta>` (necesita Pillow).
