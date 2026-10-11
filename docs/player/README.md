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
- **Importar en Unity:** Sprite Mode *Multiple*, 16 píxeles por unidad, filtro *Point*, compresión *None*. En el Sprite Editor, *Slice* → *Grid By Cell Size* (16×16, o 24×24 para la muerte) con pivote *Center*.
- `frames/` tiene los mismos cuadros sueltos por si prefieres arrastrarlos uno a uno.
- `generar_jugador.py` vuelve a generar todo: `python3 generar_jugador.py <carpeta>` (necesita Pillow).
