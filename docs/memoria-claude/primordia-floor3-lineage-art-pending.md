---
name: primordia-floor3-lineage-art-pending
description: "Tanda de arte de los linajes del piso 3 HECHA y aprobada (2026-11-03, Opus) en floor3-lineage-art/; falta importarla e integrarla (Sonnet, 4b)"
metadata:
  type: project
---

**HECHA Y APROBADA (2026-11-03, Opus), sin commit:** todo en `docs/multimedia-temp/floor3-lineage-art/` (README con hojas, ms, pivotes y
reglas de montaje; generador `generar_arte_linajes.py`; escenas de comparación en `gif/escenas/`). Fila en `docs/MULTIMEDIA.md`.
**Falta (Sonnet, en el 4b):** menú de importación, usar las variantes según el linaje y montar las reglas del README (Idle de 8 cuadros
en los charcos de la Sanguijuela, coágulos y gusanillos sobre la banda, matas al azar entre 8, nivel de acecho 55 %/hundido 80 %,
barro que se seca por tiempo, terrones a cada paso). Después, documento interactivo del piso 3 y GDD.
Cambios sobre lo pensado: las repisas de hongo se cambiaron por **estrellas de tierra** (desde arriba no se leían); el Eryops **no lleva
capa por animación**, usa el lodo por shader del pulido + textura seca; la sangre de la banda va en **coágulos aparte** (pintada en los
sellos salían rayas); se añadieron gusanillos nadando en la banda (pedido de la usuaria) y la escena del Diplocaulus con Piel anfibia.
**Añadido y aprobado el mismo día: Cordyceps (Zombi)** (README §6, `Zombie/`): el enemigo muerto se levanta 4 s aliado con el mismo diseño,
tinte pálido por shader, ojos verde-azulados, 2-3 hongos por arriba de la silueta, estela de esporas al moverse/atacar, brote al
levantarse (muerte al revés) y desmoronarse al caer. Vale para todos los enemigos MENOS jefes y trampas (planta carnívora y efímeras).
Pendiente de confirmar con la usuaria en el 4b: vida al levantarse (propuesta 50 %).

---

Decidida con la usuaria el 2026-11-03 al cerrar el paso 4a (cambios de diseño en [[primordia-floor3-mutations-decisions]]). Se hace en **otra
sesión con OPUS**, con la skill `primordia-sprites` ([[primordia-sprites-skill]]), una sola tanda ([[primordia-token-saving]]). **Preguntar
a la usuaria antes de dibujar** y hacerle las preguntas de diseño de cada pieza (ella lo pidió así; tiene detalles extra en mente).
Seguir [[primordia-boss-art-workflow]]: muestra en 1 dirección, GIF de revisión, comparar versiones si duda. El arte nuevo va a
`docs/multimedia-temp/` (carpeta propia, con README y generador `.py`) y la integración en Unity la hace Sonnet después. Sonido: aparte, más adelante.

**Piezas (todas del piso 3):**
1. **Chorro de vinagre VENENOSO — solo con el linaje Pulmonoscorpius** (único venenoso que lleva el Chorro; Xenacanthus y Cordyceps usan el
   Chorro normal). Variante visual del chorro, sus impactos y su remate (base: `floor3-jet/`, `floor3-bodies/`); el chorro de este linaje envenena y vuelve tóxico el metano.
2. **Eryops (Peonza + Patas de salto + Piel anfibia + Saco de metano), solo con ese linaje:**
   a) el jugador **cubierto de lodo** = capa sobre los 3 cuerpos (rana, Diplocaulus, vinagrillo) que se ve mientras dura el efecto (en el lodo y 5 s
      después de salir); **aprovechar y retocar la animación de hundirse en el lodo que ya existe** (`PlayerMudSink`), idea de «cazador acechando
      a su presa»: en el lodo (normal o profundo) va medio hundido, sin hundirse de verdad;
   b) un **marcador visual** de que el +30 % de velocidad está activo (que el jugador sepa cuándo tiene la mejora).
3. **Matas de hongo NUEVAS del piso 3** para «Colonizar la sala» (clase Hongo: hoy `MyceliumPatch` con el arte del piso 1). Solo piso 3; la usuaria
   pensó en tamaños **mediano y grande**. Preguntar diseño y tamaños. Frenan y dañan a los enemigos; brotan al empezar el combate y cuando
   muere un enemigo sobre una mata (máx. 6).
4. **Parásito GRANDE, solo con el linaje Lamprea:** versión más grande del parásito pegado al enemigo (hoy `Parasite_Attached`, 4 cuadros en bucle,
   12×12; en `floor3-mutations/Parasite/`). Preguntar si también cambia el salto (`Parasite_Leap`, 16×16).
5. **Sanguijuela (Escupitajo venenoso + Patas hilanderas + Piel anfibia + Saco de metano), solo con ese linaje:** el charco del Escupitajo con otra textura,
   **ligeramente rojiza de sangre y con parásitos nadando**; la **bola** del Escupitajo y su **explosión** en la misma línea, y el **charco que deja al
   explotar** con la misma textura (lo que hay hoy: el charco salpicado `Splat*` y el grande `Puddle*` del `ReactionKit`, bola y reventón en `floor3-spit/`).
   Con este linaje el Escupitajo **no arde** (no hace nube tóxica con fuego).

**Reglas de código ya decididas para estas piezas (4b, Sonnet):** Eryops sustituye a «Emboscada del fango»; Ciempiés deja un charco de 4 s del
tamaño de las vainas venenosas al aterrizar; al explotar la bola de la Sanguijuela saltan parásitos a los enemigos más cercanos hasta llenar el máximo (3);
el charco sangriento hace más daño que el normal.

**Why:** la usuaria quiso que los toques de los linajes se vean y se lean en pantalla sin ser un efecto invisible.
**How to apply:** al abrir una sesión con Opus, leer este archivo, avisar del cambio de modelo y preguntar las dudas de cada pieza antes de dibujar.

**Respuestas de la usuaria (2026-11-03, sesión Opus):** chorro = vinagre paja con veta y gotas verde veneno (impacto y salpicadura con
manchas verdes humeantes); Eryops = capa de barro en la mitad baja con goterones que se seca en 5 s, en el lodo medio hundido fijo
(lomo y ojos fuera) y marcador = gotas de barro que salpican de los pies al moverse; **solo el cuerpo de la Peonza (Diplocaulus)**, que es
el único que activa el linaje; hongos = micelio bioluminiscente (hilos blancos, setillas verde-azuladas, esporas; brota, late, pulsa al
dañar), mediana y grande; parásito Lamprea = pegado 16 px y salto 20 px con anillo de dientes; Sanguijuela = verde turbio con nubes de
sangre y 2-3 gusanillos nadando, bola translúcida con sombra que se retuerce, explosión con gotas rojizas y gusanillos. Orden: parásito →
chorro → Sanguijuela → hongos → Eryops.
**Escenas (pedido de la usuaria):** cada pieza lleva una escena GIF que compare lo normal con la versión del linaje, en
`floor3-lineage-art/gif/escenas/` (más una tira `_cuadros.png` con cuadros clave). Generador: `floor3-lineage-art/generar_arte_linajes.py`.
