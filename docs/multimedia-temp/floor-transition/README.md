# Sala de paso: la fosa

Generado con `generar_fosa.py` (`python generar_fosa.py`, necesita Pillow). 16 píxeles por unidad, filtro Point, sin compresión.

| Archivo | Cuadros | Tamaño | Uso |
|---|---|---|---|
| `Transition_Pit.png` | 4 | 112×80 (7×5 tiles) | La fosa en 3/4: borde de roca con labio claro, pared interior con estratos y la luz azul del fondo, que late (unos 6 cuadros por segundo, en bucle). Pivote en el centro |
| `Transition_Bubble.png` | 2 | 8×8 | Burbuja que sube del fondo; el código la mueve y la desvanece |
| `UI_Map_Transition.png` | 1 | 9×9 | Icono de la sala de paso en el minimapa y el mapa grande |

Los temas de la sala (Shore, intermedio, y Cambrian, el del piso 2) están en `../environment/` y se generan con `generar_tema_cambrico.py`.

**Importar en Unity:** menú *Primordia > Floor transition > Crear sala de paso y escena de prueba* (importa los temas, la fosa y el icono, crea el prefab `TransitionPit`, la plantilla `Room_TransitionCave` y la escena `Test_FloorTransition`, y añade la sala de paso a `Floor_1`).
