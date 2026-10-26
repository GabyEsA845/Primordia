# Sprites de efectos visuales

Pixel art a 16 PPU, filtro *Point*, compresión *None*. Cada hoja es una tira horizontal (*Slice* por tamaño de celda, pivote *Center*). `frames/`, `gif/` y `vista_previa_efectos.png` son para revisar.

| Hoja | Cuadros | Celda | Bucle | Uso |
|------|---------|-------|-------|-----|
| `Impact_Player.png` | 4 | 12×12 | No | Cuando un disparo del jugador golpea (destello lima) |
| `Impact_Enemy.png` | 4 | 12×12 | No | Cuando un disparo enemigo golpea al jugador o choca con una pared (destello rosa) |
| `EnemyDeath_Bubbles.png` | 6 | 24×24 | No | Burbujas que suben y revientan; se pone encima de la muerte de cada enemigo |
| `DodgeTrail.png` | 4 | 16×16 | No | Silueta translúcida que deja la célula mientras esquiva (soltar una cada ~0,05 s) |
| `Poison.png` | 4 | 12×12 | Sí | Burbujas verdes sobre el enemigo envenenado |
| `InkCloud.png` | 9 | 96×96 | No | Vejiga de tinta: aparece, se mantiene y se disipa (unos 1 s, como el prefab) |
| `Shockwave.png` | 6 | 128×128 | No | Glándula de choque: anillo que se expande (unos 0,35 s) |

**Ojo con la escala de la tinta y la onda:** `AbilityMutation.SpawnEffect` pone `localScale = radius`, pensado para el círculo provisional. Estos sprites ya están dibujados a su tamaño real (tinta de radio 3 = 96 px; onda de radio 4 = 128 px). Para que el pixel art no se agrande, la escala debería ser `radius / 3` para la tinta y `radius / 4` para la onda (o 1 si el radio no cambia). Además, `FadeOutEffect` hace crecer el efecto hasta 1,2×; con la animación ya no hace falta.

`generar_efectos.py` vuelve a generar todo: `python3 generar_efectos.py <carpeta>` (necesita Pillow).
