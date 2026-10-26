# Sprites de proyectiles

Todos apuntan a la **derecha**: el código (`Projectile.cs`) ya los rota hacia donde vuelan. Cada hoja es una tira horizontal; `frames/`, `gif/` y `vista_previa_proyectiles.png` son para revisar.

**Importar en Unity:** Sprite Mode *Multiple*, 16 PPU, filtro *Point*, compresión *None*, *Slice* por tamaño de celda y pivote *Center*. Los tamaños ya coinciden con los de los prefabs actuales, así que al poner el sprite hay que dejar la escala del objeto en **1** (hoy los prefabs escalan un cuadrado de 16 px).

| Hoja | Cuadros | Celda | Prefab | Animación |
|------|---------|-------|--------|-----------|
| `Projectile_Player.png` | 2 | 8×8 | Projectile_Player | Gota de citoplasma con estela (bucle) |
| `Projectile_Mandibles.png` | 3 | 12×12 | Projectile_Mandibles | El mordisco se cierra (sin bucle; dura 0,35 s) |
| `Projectile_Stinger.png` | 3 | 12×6 | Projectile_Stinger | Brillo que recorre la púa (bucle) |
| `Projectile_Spore.png` | 4 | 10×10 | Projectile_Spore | Gira despacio (bucle) |
| `Projectile_Enemy.png` | 2 | 10×10 | Projectile_Enemy | Late (bucle) |
| `Projectile_BossSpiral.png` | 4 | 10×10 | Projectile_BossSpiral | Mini virión que gira (bucle) |
| `Projectile_BossBurst.png` | 2 | 14×14 | Projectile_BossBurst | Orbe grande que late (bucle) |

`generar_proyectiles.py` vuelve a generar todo: `python3 generar_proyectiles.py <carpeta>` (necesita Pillow).

## Mordida de Mandíbulas (nueva, cuerpo a cuerpo)

Reemplaza a `Projectile_Mandibles` ahora que las Mandíbulas son una mordida de poco alcance y no un disparo (el sprite anterior se conserva por si acaso).

| Hoja | Cuadros | Celda | Animación |
|------|---------|-------|-----------|
| `Mandibles_Bite.png` | 5 | 24×24 | Las mandíbulas se abren, se cierran de golpe (el daño encaja en el cuadro 3) y quedan cerradas con un destello |

- Apunta a la derecha: rótala hacia donde apunta el jugador y colócala delante de la célula, a la distancia del alcance de la mordida.
- Sin bucle. Duraciones sugeridas: 60, 60, 50, 90 y 90 ms (unos 0,35 s en total).
- `generar_mordida.py` la vuelve a generar.
