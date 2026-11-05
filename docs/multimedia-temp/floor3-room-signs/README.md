# Letrero de las salas con tema (piso 3)

Al entrar **por primera vez** en una sala con tema (paso 6, bloque 1), arriba y en el centro de la pantalla baja un
tronco de Lepidodendron con el nombre de la sala y el icono del tema a cada lado. Aprobado por la usuaria el 2026-11-04.

Regenerar: `python generar_letreros_piso3.py` (todos los temas) o `python generar_letreros_piso3.py <Tema>`.

## Hojas

| Hoja | Tamaño | Uso |
|---|---|---|
| `UI_RoomSign_Log.png` | 248×42 | El tronco (común a todos los temas): cojinetes en rombo, punta izquierda cortada, derecha astillada, musgo, 2 cortes de rama de madera clara donde se apoyan los iconos |
| `UI_RoomSign_Title_<Tema>.png` | ~130-170 × 21 (26-27 con tilde) | El nombre ya rotulado: Jersey 10 a 20 px, relleno ámbar en 4 bandas, contorno y sombra (como los rótulos de jefe del piso 3) |
| `UI_RoomSign_Icon_<Tema>.png` | 16×16 | El icono del tema; se pone el mismo en los dos lados, sin voltear |

Temas (clave de archivo → `EncounterTheme.name`): `BeetleNest` Nido de bombarderos (escarabajo desde arriba),
`CalamitesMeadow` Pradera de Calamites (tallo con verticilos), `ToadPond` Charca de sapos (sapo de frente),
`SpiderLoom` Telar de la araña (araña desde arriba), `DragonflySky` Cielo de libélulas (libélula desde arriba).

Importar: *Single*, 16 PPU, *Point*, sin compresión, pivote *Center*. Es interfaz (Canvas 480×270, escala 1:1).

## Montaje (para el código)

Coordenadas en píxeles dentro del tronco (origen arriba a la izquierda):
- **Nombre:** centrado en horizontal; su línea base cae a la misma altura en todos: la parte de arriba de un nombre
  sin tilde (21 px de alto) va en y = 9; si la imagen es más alta (tildes), se sube lo que sobra (y = 9 − (alto − 21)).
- **Iconos:** centrados en (29, 21) y (220, 21), sobre los cortes de rama.
- **En pantalla:** centrado en horizontal, con la parte de arriba del tronco en y = 18. Así no toca el minimapa
  (arriba a la derecha, desde x ≈ 376) y solo pisaría la vida a partir del 7.º corazón (cada uno ocupa 18 px desde x = 8).
- **Animación (por código):** espera ~0,5 s tras cerrarse la sala, baja en 0,35 s con un rebote pequeño (sobrepasa
  ~5 px y vuelve), se queda ~2,3 s y sube acelerando en 0,35 s. Ver `gif/escena_letrero_<Tema>.gif`.

`maqueta_letrero_<Tema>.png`: el letrero sobre la sala de muestra del pantano (×2). `vista_previa_letreros.png`:
tronco, letreros e iconos a ×4.
