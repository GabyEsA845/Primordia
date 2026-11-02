# Hoja de estilo del piso 3: pantano carbonífero

Prueba de estilo previa al arte del piso 3. **No se importa a Unity.** Fija las paletas y comprueba la escala de 32 px.
Cuando se aprueba, sus valores pasan a `.claude/skills/primordia-sprites/references/pisos.md` (sección del piso 3)
y las piezas buenas (suelo, helecho, charcos, rana) se llevan a sus generadores definitivos.

| Archivo | Qué es |
|---|---|
| `hoja_estilo_piso3.png` | Rampas: tres tramos del incendio, terreno y plantas, reacciones, clase Parásito y criaturas |
| `lamina_escala.png` | Un trozo de sala de cada tramo a ×3, con la rana (32 px) y el placodermo del piso 2 (24 px) |
| `Escena_Swamp.png`, `Escena_Smoke.png`, `Escena_Burnt.png` | Los mismos trozos a 1:1 (tamaño real) |
| `Prueba_Frog_Idle_side.png`, `vista_previa_rana.png` | Rana venenosa en reposo (4 cuadros, 32×32, ~160 ms), amarilla con manchas azules: prueba de cuerpo, no definitiva |
| `muestrario_variedad.png` | Variedad: 5 helechos (frondoso, ralo, joven con báculos, inclinado, arborescente/Psaronius) en los 3 tramos, 7 formas de charco (lodo, agua, veneno), lodo profundo y detalles del suelo por tramo |
| `vista_previa_metano.png`, `gif/Prueba_Metano.gif` | Metano como niebla baja a ras de suelo (8 cuadros, ~140 ms): trama que se deshace, vetas que se arrastran, volutas y burbujas |

**Variedad (petición del usuario):** cada pieza del escenario sale de un generador con parámetros (tipo, forma, semilla,
volteo, parte quemada), así cada sala combina variantes distintas. Los charcos usan `blob()` (metabolas con forma:
redondo, alargado, lobulado, cadena, isla, salpicado, rinon). No hay flores en el escenario: las únicas son las
flores moradas del camino secreto, para que destaquen.

**Paredes de vegetación:** tapa = espesura vista desde arriba (matas de frondas en 3 capas; en la tierra quemada,
ramas carbonizadas y ceniza); cara = sombra de la espesura con frondas lejanas, troncos de Lepidodendron (cojinetes en
rombo con su cicatriz), cortina de frondas, musgo y raíces colgando y helechos al pie; en el bosque humeante, humo
entre las hojas; en la tierra quemada, bosque quemado con fondo (humo arriba, resplandor de incendios lejanos abajo, siluetas de troncos lejanos partidos, velo de humo), troncos de pie con algunas juntas encendidas y algún tronco caído suelto al pie (no apilados: parecían una pared). La
espesura se sale hacia el suelo (el borde no es recto). **Maleza quemable:** hueco de 2 tiles tapado por una maraña de
tallos secos claros (`BRUSH`), con un paso oscuro detrás; se distingue de la pared sólida a primera vista.

**Animaciones previstas (tanda 1):** burbujas en el lodo, helechos que se mecen (desfasados), destellos en el agua,
goteo del musgo, niebla de metano, vapor de fumarolas, odres que respiran, ascuas que parpadean, humo de tocones e
insectos ambientales que huyen (decorativos).

Regla del piso: el escenario va apagado y oscuro (oliva, pardo, ceniza) y lo que importa para jugar va saturado y
claro (veneno lima, fuego naranja, metano cian pálido, ámbar), así las reacciones se leen en cualquier tramo.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_hoja_estilo.py`
