# Cucaracha ladrona (piso 3)

Cucaracha carbonífera (tipo *Archimylacris*): plana, en forma de huevo (ancha delante), pronoto en escudo con el borde
ámbar, alas con venas, antenas largas y cercos con la punta ámbar. Las mismas hojas sirven para la del **almacén de
corazones** (roba el medio corazón si sales sin enviarlo a la vaina; **no se puede matar**: el código la hace
invulnerable) y las de la sala **Ladronas** (roban biomasa; **sí se matan y sueltan lo robado**).
Regenerar: `python generar_cucaracha.py`.

**Vista desde arriba, mirando a la derecha: el código la gira hacia donde corre** (rotación del transform; con
*Point* se ve algo dentada en diagonal, aceptado). **El botín lo pone el código** como capa sin girar encima de
`LOMO` = (16, 14) de la celda de 40×28 (girar ese punto con la cucaracha). Importar: *Multiple*, 16 PPU, *Point*,
sin compresión, pivote centro (Emerge/Hide: ver abajo).

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Roach_Idle` | 4 | 40×28 | 160 | Parada, mueve las antenas |
| `Roach_Run` | 4 | 40×28 | 60 | Corre (marcha en trípode); girada hacia donde va |
| `Roach_Grab` | 5 | 40×28 | 110 | Se para, agacha el escudo, engancha el botín con las mandíbulas y se lo sube estirándose. **Cuadro que cuenta: 3** (el botín aparece en `LOMO`) |
| `Roach_Hurt` | 2 | 40×28 | 90 | Daño (solo las de Ladronas) |
| `Roach_Death` | 6 | 40×28 | 110 | Respingo, se da la vuelta, patalea panza arriba y se apaga (suelta el botín al empezar) |
| `Roach_Emerge` | 6 | 50×28 | 80 | Sale de una grieta corriendo hacia la derecha: antenas, escudo, cuerpo. **Sin girar** (con *Flip X* para salir hacia la izquierda); la grieta está en (10, 14) de la celda: pivote ahí. Al acabar, pasa a `Run` girada |
| `Roach_Hide` | 6 | 50×28 | 80 | Llega corriendo hacia la derecha y se mete de cabeza en la grieta, en (39, 14) de la celda (pivote ahí). **Cuadro que cuenta: 3** (el lomo entra: el código quita el botín). El último cuadro es la grieta sola con polvo |
| `Deco_RoachCrack` | 1 | 20×12 | | La grieta del suelo sola, para dejarla donde salen o se esconden (antes y después) |

`maqueta_cucaracha.png` y `gif/escena_cucaracha.gif`: varias corriendo giradas con el medio corazón o la biomasa a
cuestas, una saliendo de la grieta y otra cogiendo biomasa. GIF de cada animación en `gif/`.
