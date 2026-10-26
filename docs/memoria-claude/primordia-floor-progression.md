---
name: primordia-floor-progression
description: "Sala de paso, elección de Boca y metamorfosis EN MAIN (merge 1e25a7a, 2026-10-23); siguiente: piso 2. Rediseño del 2026-10-23, en main (merge 3c9c9a1): mutaciones y linajes por piso, sala de paso con Boca obligatoria y metamorfosis, sala de cambio; siguiente: feature/floor-transition"
metadata:
  node_type: memory
  type: project
  originSessionId: 5fd902a4-0bd3-4d87-8a22-abb4df8d5c29
  modified: 2026-10-24T00:18:40.088Z
---

Decidido por el usuario el 2026-10-23 tras probar la build en itch.io. Está en docs/GDD Primordia.md ("Mutaciones de cada piso", "Paso al piso siguiente", "Sala de cambio (el laboratorio de Darwin)", "Forma del jugador en cada piso", tabla "Decidido el 23 de octubre") y en docs/PLAN.md, en main desde el merge 3c9c9a1. Sustituye a "4 mutaciones por piso que se suman" de [[primordia-floors-design]].

**Diseño cerrado:**
- **Mutaciones por piso (estilo Enter the Gungeon):** el piso 1 no cambia. Desde el piso 2, elecciones, tienda y burbujas del jefe solo ofrecen mutaciones de ese piso (y mejoras de las equipadas). 8 por piso ahora (2 por ranura), 12 más adelante (3 por ranura, una Boca por familia). Cada piso con sus linajes; nunca se actualizan.
- **Dificultad:** vida de los enemigos ×1,5–2 por piso, más densidad y patrones (no esponjas). Nivel I del piso N+1 ≈ nivel II del piso N.
- **Linajes:** útiles un piso por encima, flojos dos por encima; al cambiar de piso solo se ajustan valores, nada visual.
- **Burbujas del jefe:** sin Bocas; una es siempre la mejora de una mutación equipada.
- **Sala de paso:** adyacente a la del jefe, se abre al vencerlo. Al usar el paso, ventana vistosa (más que la elección de mutación) y obligatoria con las Bocas del piso siguiente; luego cinemática de metamorfosis según la familia y paso al piso nuevo. Hasta que exista el piso 2, lleva a la victoria.
- **Nivel de la Boca nueva:** empieza en I; los niveles extra de la anterior se reparten al azar uno a uno entre las otras mutaciones que puedan subir, y **los que no caben se pierden** (decisión del usuario: evita llegar con solo la Boca mejorada).
- **Sin Bocas en el piso siguiente** (ni elecciones ni tienda), pero sí mejoras de la Boca que llevas.
- **Sala de cambio (laboratorio de Darwin):** desde el piso 2, una por piso, pegada a la tienda (la tienda gana una segunda puerta), gratis, un solo cambio por piso. Cápsula con líquido en el centro, E abre una interfaz propia: la criatura con sus ranuras (como el panel de la pausa); eliges ranura, ves las opciones del piso de esa ranura al mismo nivel con dibujo y descripción, Aceptar. Única forma de cambiar de Boca; si cambia la familia, otra metamorfosis y otro cuerpo. No existe en el piso 1.
- **Piso 2:** 2 Bocas = 2 cuerpos: Cnidocitos (proyectil → medusa) y una Boca cuerpo a cuerpo **por diseñar** (→ pez primitivo con mandíbulas). El lirio de mar (área) llega con la tercera Boca. 8–10 salas de combate (piso 1 sigue con 4–5 y 1–2 laterales). Partidas de 40–60 min.
- **"Mejoras a futuro" del PLAN:** cómo seguir consiguiendo mejoras de las mutaciones del piso 1 en pisos posteriores.

**Sala de paso (aprobada 2026-10-23, en el GDD "Cómo es la sala de paso" y "Secuencia del salto", sin commit aún):** cueva orgánica que pasa del piso actual al siguiente (paleta, decoración del piso 2, sonido); fosa central sólida con luz azul y burbujas; E en el punto del borde más cercano; salto → jugador congelado en el aire con la ventana de Bocas (sin cerrar) → reparto de niveles visible en la ventana → metamorfosis ~4 s (membrana que brilla y revienta, franjas de cine) → fundido (de momento a victoria). Orden pedido por el usuario: **antes de la sala**, diseñar la Boca cuerpo a cuerpo del piso 2 y hacer los sprites de los cuerpos medusa y pez primitivo; luego sala, paso, elección + reparto, metamorfosis (todo en esta rama). Ramas después: `feature/exchange-room` y `feature/floor-2`.

**Why:** el piso 1 se sentía corto y mezclar mutaciones de ambientaciones distintas resultaba extraño.
**How to apply:** seguir el GDD; diseño grande, así que preguntar antes de programar lo que no esté escrito. Ver [[primordia-workflow]], [[primordia-unity-gotchas]].

**Progreso en `feature/floor-transition` (2026-10-23, sin commit, pendiente de que el usuario pruebe):**
- Diseño escrito en GDD/PLAN; Placas mandibulares aprobada (Boca cuerpo a cuerpo del piso 2) y regla de que desde el piso 2 la Boca va dibujada en el cuerpo con animación de ataque propia (el usuario pidió esmerarse en los ataques).
- Arte aprobado: `docs/multimedia-temp/floor2-bodies/` (medusa y placodermo, 24×24, Idle/Move/Dodge/Attack ×3 dir, Hurt, Death; efectos de mordisco, arpón, destello). Aún sin importar en Unity (se hará con la metamorfosis).
- Sala de paso hecha (sala + paso): temas Shore/Cambrian (`environment/generar_tema_cambrico.py`), fosa (`floor-transition/generar_fosa.py`), `RoomRole.Transition`, cueva `Room_TransitionCave`, temas por celda en FloorBuilder, `TransitionPit` (E en el borde, salto, de momento victoria), icono en el mapa, menú **Primordia > Floor transition > Crear sala de paso y escena de prueba** (Test_FloorTransition), `FloorTransitionTests`.
- Commits en la rama: 927afcb (sala + paso), 60ec171 (pasillo de 14 tiles + fundido de música por distancia, Amb_Floor2_Loop, Transition_Jump/Fall).
- Ventana de Bocas (aprobada, sin commit, pendiente de prueba): MouthChoiceUI/MouthCard (criatura animada, ataque al pasar el ratón, reparto animado), MouthChange (plan/aplicar), MutationData.Floor/Family/CreatureFrames, Bocas del piso 2 en Data/Mutations/Floor2 con ataque PROVISIONAL (Stinger/Mandíbulas) hasta feature/floor-2, iconos en floor2-bodies/generar_iconos_piso2.py.
- Ventana de Bocas en d96e9b8.
- Metamorfosis (aprobada 2026-10-23, sin commit, pendiente de prueba): Metamorphosis.cs en el prefab de la fosa (abismo lejos del piso, brillo del color de la FAMILIA, silueta Morph_/Reveal_ de docs/multimedia-temp/metamorphosis, pose de ataque, fundido); saltable con E solo desde que se vio entera (PlayerPrefs Primordia.MetamorphosisSeen, aviso abajo a la derecha en CinematicOverlay). Cambio de cuerpo real: PlayerAnimator.SetCreatureController manda sobre Piel; Attack_* al disparar; MutationLook.CreatureForm oculta piezas; HoldForm/ReleaseForm. Victoria con la criatura (GameUI).
- **Rama cerrada:** feature/floor-transition en main (merge 1e25a7a, 2026-10-23), push hecho. Siguiente: piso 2; el diseño y sus sprites siguen en [[primordia-floor2-design]].
- ProjectSettings.asset: Unity quitó InputSystem_Actions de preloadedAssets; no lo incluyo en commits (avisado al usuario).
