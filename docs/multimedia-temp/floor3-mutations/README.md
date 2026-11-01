# Mutaciones del piso 3 (pantano carbonífero)

Generadas con `generar_mutaciones_piso3.py` (skill primordia-sprites; importa `../floor3-enemies/generar_enemigos_piso3.py`
para las formas sombreadas y la telaraña):

```
python generar_mutaciones_piso3.py limbs skins organs specials
```

**Regla del piso: mutaciones por niveles.** Para no ensuciar el sprite del jugador:

1. **Extremidades y Órganos (normales):** no cambian la silueta; se ven con **efectos al usarlas**.
2. **Pieles:** retocan el cuerpo **solo dentro de su silueta** (ni contorno ni ojos).
3. **Especiales del ámbar:** sí cambian el modelo, cada una en su zona (Cercos detrás, Cápsula en el lomo, Gota delante).
4. **Linajes:** la transformación (`../floor3-lineages`).

Las capas (Pieles y Especiales) se calculan desde los sprites de `../floor3-bodies` cuadro a cuadro: si se rehace un
cuerpo, se vuelven a lanzar. Importar: *Multiple*, 16 PPU, *Point*, sin compresión, pivote *Center* salvo que se diga.

## Capas sobre el cuerpo

| Hoja | Lienzo | Orden | Uso |
|---|---|---|---|
| `Skin/Skin_<AmphibianSkin\|CharredScales>_<Cuerpo>_<Anim>_<side\|down\|up>.png` | igual que la hoja del cuerpo (32 o 40) | encima | Piel: solo los píxeles del cuerpo que cambian. Anims: Idle, Move, Dodge y los ataques; el Diplocaulus también `Spin`, `SpinEnd`, `Dizzy` (sin dirección). Mismos cuadros y ms que el cuerpo |
| `Special/Special_Cerci_<Cuerpo>_<Anim>_<dir>.png` | 56×56, cuerpo centrado | **detrás** (de espaldas, `up`: delante) | Cercos de cucaracha; punta de ámbar encendida = quiebro listo. Gastado: el código tiñe la capa más oscura (≈ 0,55) hasta la recarga |
| `Special/Special_AmberCapsule_<Cuerpo>_<Anim>_<dir>.png` | 56×56 | encima | Costra de resina en el lomo con un mosquito atrapado. Usada en la sala: el código la tiñe apagada |
| `Special/Special_AmberGland_<Rest\|Full>_<Cuerpo>_<Anim>_<dir>.png` | 56×56 | encima | Glándula de la Gota: garganta (rana), puntas de la cabeza que gotean (Diplocaulus), entre las pinzas (vinagrillo). El código funde `Rest` → `Full` según la carga |

`<Cuerpo>`: `Frog`, `Diplocaulus`, `Whipscorpion`. `<Anim>` de las especiales: Idle, Move, Dodge y los ataques con dirección
(Frog `Attack`, Diplocaulus `AttackStart`, Whipscorpion `AttackStart/Loop/End`). Sin capa en Hurt ni Death.

## Efectos (`Effects/`) e iconos (`Icons/`)

| Hoja | Cuadros | Celda | Uso |
|---|---|---|---|
| `Fx_JumpingLegs_Takeoff` | 5 | 32×24 | Patas de salto: despegue (polvo y muelle verde) en el punto de salida |
| `Fx_JumpingLegs_Land` | 5 | 32×16 | Aterrizaje: anillo de polvo y gotas |
| `Fx_JumpingLegs_Quake` | 5 | 48×32 | II: onda de 3 de daño con grietas |
| `Fx_Stunned` | 4 (bucle) | 16×10 | III (Rana toro) y cualquier aturdido: estrellitas sobre la cabeza |
| `Fx_SilkThread_Start` | 4 | 16×16 | Patas hilanderas: estallido de seda al salir |
| `Fx_SilkThread` | 2 (bucle) | 8×4 | Tramo del hilo que se repite entre la salida y la llegada (5 s) |
| `Fx_SilkThread_Anchor` | 2 | 8×8 | Nudo de cada extremo |
| `Fx_SilkThread_Hit` | 4 | 12×12 | El hilo daña a un enemigo |
| `Fx_SilkWeb_Appear` / `_Idle` / `_Fade` | 3 / 1 / 3 | 32×32 | III: telaraña en el cruce (rocío dorado = tuya) |
| `Fx_AmphibianSkin_Charge` | 4 (bucle) | 32×32 | En agua o charco: el moco se carga |
| `Fx_AmphibianSkin_Shield` | 4 (bucle) | 48×48 | Moco cargado alrededor del cuerpo |
| `Fx_AmphibianSkin_Absorb` | 4 | 48×48 | Absorbe el golpe y revienta en gotas |
| `Fx_AmphibianSkin_Reflect` | 4 (bucle) | 48×48 | III: la película destella (refleja) |
| `Fx_CharredScales_Primed` | 4 (bucle) | 32×32 | Tras pisar fuego, 4 s: ascuas que suben (tus ataques queman) |
| `Fx_Burning` | 4 (bucle) | 16×16 | **Genérico**: enemigo ardiendo |
| `Fx_CharredScales_Blaze` | 6 | 48×48 | III (Brasa): estallas en llamas al recibir un golpe |
| `Fx_MethaneSac_Release` | 7 | 48×48 | Saco de metano: la vejiga brota, se hincha y revienta; **la nube sale en el cuadro 5** |
| `Fx_MethaneSac_Cloud` | 6 (bucle) | 40×32 | Nube a la deriva 6 s |
| `Fx_MethaneSac_CloudFade` | 4 | 40×32 | Se deshace |
| `Fx_MethaneSac_Explode` | 8 | 64×64 | Explota con cualquier ataque (6 de daño) |
| `Fx_GroundFire` | 6 (bucle) | 32×24 | **Genérico**: fuego en el suelo (III Incendio, 3 s; reacciones) |
| `Fx_Pheromones_Release` | 7 | 72×72 | Feromonas: onda y velo rosa coral de 2 u; radio máximo en el cuadro 4 |
| `Fx_Pheromones_Aura` | 4 (bucle) | 32×32 | Sobre cada enemigo confundido |
| `Fx_Pheromones_Mark` | 4 (bucle) | 14×12 | Espiral sobre su cabeza |
| `Fx_Pheromones_Frenzy` | 4 (bucle) | 14×12 | III (Frenesí): espiral y signo de furia |
| `Fx_Cerci_Flash` | 4 | 32×32 | Quiebro (la silueta fantasma: el sprite del cuerpo teñido de ámbar y translúcido) |
| `Fx_Cerci_Ready` | 4 | 16×16 | El quiebro vuelve a estar listo |
| `Fx_AmberBlock_Form` / `_Hold` / `_Crack` | 4 / 2 / 2 | 48×48 | Bloque de ámbar: Cápsula (te encierra 2 s) y Gota (encierra al enemigo 4 s); el código lo escala al tamaño de lo encerrado. Apoyo abajo en el centro |
| `Fx_AmberBlock_Shatter` | 6 | 64×64 | Revienta en esquirlas que dañan y empujan |
| `Proj_AmberDrop` | 2 (bucle) | 10×10 | Gota de ámbar lanzada |
| `Fx_Parasite_Drain` | 2 (bucle) | 8×8 | Clase Parásito: gota de vida que viaja del enemigo al jugador |
| `Fx_Parasite_Heal` | 6 | 32×32 | Cada 15 chupados: ½ corazón |
| `Parasite/Parasite_Attached` | 4 (bucle) | 12×12 | Parásito pegado al enemigo (hasta 3) |
| `Parasite/Parasite_Leap` | 5 | 16×16 | Salta al siguiente cuando muere el huésped (el código lo mueve en arco) |

Duraciones orientativas: bucles 90-120 ms, golpes y estallidos 60-80 ms.

Iconos `Icon_<Mut>_32` y `Icon_<Mut>_16`: `JumpingLegs`, `SpinnerLegs`, `AmphibianSkin`, `CharredScales`, `MethaneSac`,
`Pheromones`, `ClassParasite`, `Cerci`, `AmberCapsule`, `AmberDrop`.

`vista_previa_*.png` y `gif/` son solo para revisar (`vista_previa_especial_las_tres.png`: las tres especiales a la vez).
