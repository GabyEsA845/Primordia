# Escupitajo venenoso, segunda versión (piso 3, rana)

Generado con `generar_escupitajo.py` y la skill `primordia-sprites`. Sustituye a `Proj_PoisonSpit`, `Proj_PoisonSpitSmall`,
`Fx_PoisonPuddleSmall_*` y `Fx_PoisonSpit_Burst` de `floor3-bodies/Effects/` (esas hojas se quedan allí, sin usar por el Escupitajo).
`Fx_PoisonPuddleBig_*` sigue siendo el charco de las **reacciones**; el Escupitajo deja ahora su propio charco salpicado.

La bola **rueda por el suelo** y deja una **banda de baba continua**, como el E de Kog'Maw. No encoge: mantiene su tamaño y en los
últimos 0,6 s se hincha, parpadea y revienta.

## Hojas (`Effects/`)

| Hoja | Cuadros | Celda | ms | Uso |
|---|---|---|---|---|
| `Proj_PoisonSpit_Roll_side/down/up` | 8 | 24×24 | 135 | Bola rodando, en bucle (una vuelta = 1,08 s, lo que tarda en recorrer su contorno a 2 u/s). La luz no gira; giran las manchas. Lleva pie pegajoso y sombra de contacto dibujados |
| `Proj_PoisonSpit_Swell_side/down/up` | 6 | 24×24 | 100 | Últimos 0,6 s de vida: sigue rodando, crece hasta 1,3, recoge la cola, parpadea en los cuadros 2, 4 y 5. Al acabar, `Fx_PoisonSpit_Burst` |
| `Proj_PoisonSpitSmall_Roll_side/down/up` | 4 | 12×12 | 110 | Bolitas de la Camada (III), en bucle |
| `Fx_PoisonSpit_Burst` | 7 | 64×64 | 60 | Reventón irregular de radio 1,5 u: chorretones hacia tres lados, gotas desiguales que se estiran y caen. **Cuadro que cuenta: el 1** |
| `Fx_PoisonSplat_Appear/Idle/Fade` | 3/4/3 | 64×64 | 90 | Charco salpicado al reventar. **El cuerpo ocupa ~¾ del dibujo** (radio dibujado del cuerpo ≈ 14 px); las lenguas y gotas sobresalen. Escalar para que el cuerpo coincida con el radio que hace daño |
| `Fx_SlimeTrail_Rim_0…5` y `Fx_SlimeTrail_Body_0…5` | 8 | 20×20 | ver abajo | Sellos de la banda (~10 px de ancho), en dos capas. 6 variantes |
| `Fx_SlimeTrailThin_Rim_0…4` y `Fx_SlimeTrailThin_Body_0…4` | 8 | 14×14 | ver abajo | Sellos de la banda fina de la Camada (~4-5 px). 5 variantes |
| `Fx_SlimeTrail_Shine` | 4 | 8×8 | 140 | Brillo húmedo que titila, en bucle, encima de algunos sellos |

## Pivotes

- **Bolas:** el pivote es el **punto de apoyo en el suelo**, no el centro: (12, 16,5) en la celda de 24 → pivote normalizado
  **(0,5, 0,3125)**; en la de 12, (6, 8,5) → **(0,5, 0,29)**. La posición del proyectil es la del suelo, donde se pintan los sellos.
- Sellos, brillo, reventón y charco: centro.
- Hacia la izquierda, `side` con *Flip X*. Las diagonales usan `side` (se elige por el eje dominante de la velocidad: |vx| ≥ |vy| → side).

## Banda de baba: cómo se monta en el juego

- **Un sello cada 0,25 u** recorridos (la fina, cada 0,125 u). Variante al azar: en la normal, la 5 (goterón ancho) sale ~8 % de las
  veces y las otras repartidas; en la fina, la 4 ~12 %. Desplazamiento lateral: normal, −1/0/0/+1 px al azar; la fina serpentea
  (`1,3 · sin(distancia_px / 7 + fase) ± 0,45 px`, redondeado). Posiciones **redondeadas al píxel**.
- **Dos capas** (así la unión de sellos no deja costuras en ningún ángulo): todos los `Rim` en un orden de dibujo por debajo de **todos**
  los `Body`; los brillos encima. Dentro de cada capa, el sello más nuevo encima. Opacos (no translúcidos: se apilarían).
- **Cuadros de cada sello** (los mismos índices en `Rim` y `Body`): 0 y 1 al nacer (90 ms cada uno; más grueso y brillante: es la cola
  pegada a la bola), 2 húmedo (se mantiene), 3-7 se seca (100 ms cada uno, los últimos 0,5 s de su vida). Cada tramo se seca a los
  **3 s (4 s en el nivel II) desde que se pinta**, así que la banda desaparece desde la punta.
- **Brillo:** sobre ~1 de cada 4 sellos (al azar) mientras está en el cuadro 2, desplazado 2 px arriba a la izquierda.
- **Colisión:** la banda es continua: el charco de veneno actúa a lo largo de toda ella, no en charcos sueltos.

## Revisión

`vista_previa_bola.png`, `vista_previa_banda.png`, `vista_previa_reventon.png`, `fondos_banda.png` (sobre los tres tramos) y `gif/`
(`gif/escenas/escena_direcciones.gif` y `escena_camada.gif` simulan el juego). Solo para revisar.

Regenerar: `"$LOCALAPPDATA/Programs/Python/Python312/python.exe" generar_escupitajo.py`
