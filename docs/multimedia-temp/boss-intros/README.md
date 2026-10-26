# Presentación de los jefes

Generado con `generar_presentaciones.py` (`python generar_presentaciones.py`, necesita Pillow). Reutiliza las funciones de dibujo de los generadores de cada jefe (`../enemies/`, `../enemies/BossAmoeba/`, `../floor2-bosses/`) sin volver a escribir sus archivos.

Al entrar en la sala de un jefe (unos 3 s; siempre; se salta con **E**): el jugador queda quieto e invulnerable, la cámara va al jefe, el jefe hace su **pose**, entra el **rótulo** con el nombre, el título y el **retrato**, y la cámara vuelve al jugador.

## Retratos (`Portraits/`)

176×176 (≈ 1/4 de la pantalla de 480×270), **misma escala de píxel que el juego**: en la UI se muestran a 1 píxel de imagen por píxel de pantalla de referencia. Fondo transparente.

| Archivo | Jefe | Título |
|---|---|---|
| `Portrait_Virus` | Virus gigante | Plaga primigenia |
| `Portrait_Amoeba` | Ameba gigante | La que todo lo engulle |
| `Portrait_Anomalocaris` | Anomalocaris | Terror del Cámbrico |
| `Portrait_Eurypterid` | Euriptérido | Escorpión de los mares |

## Poses (`Poses/`)

Hojas en tira con la celda de cada jefe (importar como sus otras hojas: *Multiple*, 16 PPU, *Point*, sin compresión, pivote *Center*). 8 cuadros a **130 ms**; el último cuadro se mantiene hasta que se va el rótulo.

| Hoja | Celda | Qué hace |
|---|---|---|
| `Virus_Intro` | 48×48 | Las espículas giran y se encienden, abre el ojo de golpe y ruge temblando |
| `Amoeba_Intro` | 112×112 | Se encoge, crece de golpe enfadándose y ruge temblando |
| `Anomalocaris_Intro` | 112×112 | Se acerca nadando, abre los apéndices, se le encienden los ojos y tiembla |
| `Eurypterid_Intro` | 128×128 | Levanta y abre las pinzas, eriza las púas del caparazón y se le ponen rojos los ojos |

## Rótulo

- Banda diagonal oscura de lado a lado con un filo de color arriba y abajo; nombre a la izquierda, título debajo, retrato a la derecha sobre la banda; «[E] Saltar» pequeño abajo a la derecha.
- **Nombre:** tipografía **Jersey 10** (`fuentes/`, licencia OFL en `Jersey10-OFL.txt`; copiarla a `Assets/_Primordia/Art/Fonts` con su licencia) a 40 px, sin suavizado, relleno en 4 bandas de color de arriba abajo, contorno y sombra de 1 px. **La letra es la misma en todos los pisos; el color cambia por piso:**
  - Piso 1 (sopa): relleno lima `#f8ffd8 → #d8f070 → #a8c040 → #5a7020`, banda granate `#1e0e1a`, filo y título rosa `#ff7aa8`.
  - Piso 2 (océano): relleno turquesa `#e8fff4 → #9ff0e0 → #4cc4c8 → #2a7a9a`, banda azul `#08101e`, filo y título `#78dcd2`.
- **Título:** Silkscreen Regular 8 px.

`maqueta_rotulo_*.png` muestran el rótulo sobre la pantalla (ampliado ×2); `vista_previa_*.png` y `gif/` son solo para revisar. `prueba_fuentes_piso2.png` es la comparación de tipografías con la que se eligió Jersey 10.
