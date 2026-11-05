# Aspecto nuevo de las especiales del ámbar (piso 3)

Regenerar: `python generar_especiales_v2.py`. Importar: *Multiple*, 16 PPU, *Point*, sin compresión.

## Subtanda 1: bloque de ámbar y gota (`shell/`)

Sustituye a `Fx_AmberBlock_*` y `Proj_AmberDrop` de `floor3-mutations`. El bloque va en **dos capas** y el código
dibuja la criatura **entre ellas** (Cápsula: el jugador; Gota: el enemigo), escalando el conjunto a su tamaño.
Celda 48×56; el suelo del bloque está en y = 51 (pivote abajo-centro, en el pie de la criatura).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Fx_AmberShell_Back_Form` / `Front_Form` | 6 / 6 | 48×56 | 90 | La resina sube del suelo y se endurece (destello al final). Back detrás de la criatura, Front delante |
| `Fx_AmberShell_Back_Hold` | 1 | 48×56 | | El fondo mientras dura |
| `Fx_AmberShell_Front_Hold` | 8 | 48×56 | 120 | Delante mientras dura (bucle): un brillo cruza el bloque, burbujas |
| `Fx_AmberShell_Front_Crack` | 3 | 48×56 | | Grietas en 3 fases (el código elige: por tiempo en la Cápsula; por tiempo o golpes en la Gota) |
| `Fx_AmberShell_Shatter` | 8 | 80×72 | 70 | Revienta: destello con grietas, esquirlas que vuelan, giran y caen, polvo dorado. El bloque está en (16, 10) de la celda (mismo pie, más margen). Sin Back: la criatura queda libre desde el cuadro 1 |
| `Proj_AmberDrop` | 4 | 16×16 | 70 | La gota lanzada (mirando a la derecha; girarla con la dirección) |
| `Fx_AmberDrop_Hit` | 5 | 24×24 | 60 | Estalla al dar; después se forma el bloque |

`vista_previa_bloque.png` (antes / Cápsula con la rana / Gota con un escarabajo / grietas / estallido) y
`gif/escena_bloque_nuevo.gif` (la secuencia completa).

## Subtanda 2: nivel III (`level3/`)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Fx_Cerci_Burst_III` | 7 | 56×56 | 70 | Cercos III: estallido de chispas en estrella al quebrar, centrado en el cuerpo (en I-II sigue `Fx_Cerci_Flash`) |
| `Fx_AmberShell3_Back_Form` / `Front_Form` | 8 / 8 | 48×56 | 90 | Cápsula III se forma: el bloque base (6) y la cáscara exterior que sube por fuera (2) |
| `Fx_AmberShell3_Back_Hold` | 8 | 48×56 | 120 | Fondo con el núcleo cálido que late (detrás de la criatura) |
| `Fx_AmberShell3_Front_Hold` | 8 | 48×56 | 120 | Bloque + cáscara exterior gruesa y oscura con vetas doradas |
| `Fx_AmberShell3_Front_Crack` | 3 | 48×56 | | Grietas solo en la cáscara exterior (3 fases); revienta con `Fx_AmberShell_Shatter` |
| `Fx_CapsuleHeal` | 12 | 48×56 | 110 | Por encima de todo mientras cura: pulsos cálidos y un corazoncito que sube por cada medio curado (salen en los cuadros 1, 5 y 9: 3 medios = 1½ corazones en III) |
| `Fx_AmberCrystals_Erupt` | 6 | 48×56 | 80 | Gota III al dar: cristales que brotan del suelo y se cierran en el bloque (el cuadro 5 ya es `Fx_AmberFossil_Front`). Back: `Fx_AmberShell_Back_Hold` desde el cuadro 4 |
| `Fx_AmberFossil_Front` | 8 | 48×56 | 130 | El bloque cristalino con el cadáver (escudo): puntas de cristal y destellos que saltan |
| `Fx_AmberStun_Stars` | 6 | 32×16 | 100 | Gota I-II: estrellas de aturdido girando encima del bloque |

**Fósil (shader, Sonnet):** en la Gota III el enemigo se oscurece en ~0,8 s hacia `#3a1a08` (mezcla del color) y al
final lleva un borde de luz ámbar `#e89a30` arriba y a la izquierda (ver `fossil_tint` en el generador). GIF:
`escena_cercos_III.gif`, `escena_capsula_III.gif`, `escena_gota_III.gif`, `escena_gota_I_II.gif`;
`vista_previa_nivel_III.png`.
