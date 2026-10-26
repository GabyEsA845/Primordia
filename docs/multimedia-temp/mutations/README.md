# Mutaciones del jugador

Sprites de las 9 mutaciones, generados con `generar_mutaciones.py` a partir de `../player/generar_jugador.py` (mismas paletas, contorno y balanceo por cuadro).

- 16 píxeles por unidad, filtro Point, sin compresión, fondo transparente.
- Tiras horizontales; cada hoja se corta por cuadros del tamaño del lienzo (alto de la hoja).
- `side` mira a la derecha (para la izquierda se voltea). Las Bocas están dibujadas mirando a la derecha y el juego las gira.
- Orden de dibujo: Extremidades, Cuerpo (Piel), Órgano, Boca. Mirando hacia arriba, la Boca va detrás del cuerpo.
- Las piezas no se dibujan en Dodge ni en Death. En Hurt se tiñen de blanco y rojo desde el SpriteRenderer (no tienen cuadros propios).
- Distancia de la Boca al centro usada en la vista previa: célula 6 px, Caparazón 8.5 px, Piel urticante 7 px.
- El Órgano va en la mitad de abajo de la célula, bajo los ojos.

| Hoja | Ranura | Cuadros | Lienzo | Punto de unión |
|---|---|---|---|---|
| `Skin/Skin_Carapace_Idle_side.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Move_side.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Dodge_side.png` | Skin | 5 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Idle_down.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Move_down.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Dodge_down.png` | Skin | 5 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Idle_up.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Move_up.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Dodge_up.png` | Skin | 5 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Hurt.png` | Skin | 2 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_Carapace_Death.png` | Skin | 7 | 32×32 | centro del cuerpo (16, 16) |
| `Skin/Skin_StingingSkin_Idle_side.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Move_side.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Dodge_side.png` | Skin | 5 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Idle_down.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Move_down.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Dodge_down.png` | Skin | 5 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Idle_up.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Move_up.png` | Skin | 4 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Dodge_up.png` | Skin | 5 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Hurt.png` | Skin | 2 | 24×24 | centro del cuerpo (12, 12) |
| `Skin/Skin_StingingSkin_Death.png` | Skin | 7 | 32×32 | centro del cuerpo (16, 16) |
| `Mouth/Mouth_Mandibles_Idle.png` | Mouth | 4 | 24×24 | (0, 12): borde izquierdo a media altura, se coloca en el borde de la célula y gira hacia el cursor |
| `Mouth/Mouth_Mandibles_Attack.png` | Mouth | 3 | 24×24 | (0, 12): borde izquierdo a media altura, se coloca en el borde de la célula y gira hacia el cursor |
| `Mouth/Mouth_Stinger_Idle.png` | Mouth | 4 | 24×24 | (0, 12): borde izquierdo a media altura, se coloca en el borde de la célula y gira hacia el cursor |
| `Mouth/Mouth_Stinger_Attack.png` | Mouth | 3 | 24×24 | (0, 12): borde izquierdo a media altura, se coloca en el borde de la célula y gira hacia el cursor |
| `Mouth/Mouth_Spores_Idle.png` | Mouth | 4 | 24×24 | (0, 12): borde izquierdo a media altura, se coloca en el borde de la célula y gira hacia el cursor |
| `Mouth/Mouth_Spores_Attack.png` | Mouth | 3 | 24×24 | (0, 12): borde izquierdo a media altura, se coloca en el borde de la célula y gira hacia el cursor |
| `Limbs/Limbs_Fins_Idle_side.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_Fins_Idle_down.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_Fins_Idle_up.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_Fins_Move_side.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_Fins_Move_down.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_Fins_Move_up.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_JointedLegs_Idle_side.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_JointedLegs_Idle_down.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_JointedLegs_Idle_up.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_JointedLegs_Move_side.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_JointedLegs_Move_down.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Limbs/Limbs_JointedLegs_Move_up.png` | Limbs | 4 | 32×32 | centro de la célula (16, 16) |
| `Organ/Organ_InkSac_Idle.png` | Organ | 4 | 16×16 | centro de la célula (8, 8) |
| `Organ/Organ_InkSac_Activate.png` | Organ | 4 | 32×32 | centro de la célula (16, 16) |
| `Organ/Organ_ShockGland_Idle.png` | Organ | 4 | 16×16 | centro de la célula (8, 8) |
| `Organ/Organ_ShockGland_Activate.png` | Organ | 4 | 32×32 | centro de la célula (16, 16) |

`gif/` tiene cada animación por separado y `Criatura_1/2/3.gif` con las tres combinaciones andando en las tres direcciones.

Para regenerar: `python generar_mutaciones.py ../player/generar_jugador.py .`

## Encargo

El encargo anterior (piezas pequeñas encima de la célula) quedó sustituido por uno con cambios mucho más visibles, al estilo de *Everything is Crab*. El encargo actual está en **`PROMPT.md`**: Piel cambia el cuerpo entero y lo agranda, Boca es un arma grande que gira hacia el cursor, Extremidades son apéndices que se mueven al andar y Órgano se ve dentro de la célula.

