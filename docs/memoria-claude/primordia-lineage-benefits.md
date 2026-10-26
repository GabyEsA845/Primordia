---
name: primordia-lineage-benefits
description: "Beneficios de linajes aprobados el 2026-10-15 (base por clase + toque por animal), pendiente de programar en feature/lineage-benefits"
metadata:
  node_type: memory
  type: project
  originSessionId: 8d59c261-1e60-421a-9039-00c7c87f936d
  modified: 2026-10-15T13:28:32.800Z
---

Decidido por el usuario el 2026-10-15 (antes de programar; rama prevista `feature/lineage-benefits`):

- **Estructura:** base común por clase + un toque propio pequeño por animal. Beneficio **fijo** (no escala con niveles). Dura mientras lleves las 4 mutaciones, también al cambiar de piso.
- **Interfaz:** cartel breve al completarlo (junto al destello dorado) y texto en la pausa. No en tarjetas ni tienda.
- **Bases:** Carnívoro = Rematar (+100 % de daño a enemigos bajo el 30 % de vida; contra el jefe +50 %). Herbívoro = Pastar biomasa (cada 10 de biomasa recogida cura medio corazón; con vida llena da medio corazón verde temporal que absorbe un golpe, máximo uno). Venenoso = Toxina débil (envenenados reciben +50 % de daño de cualquier fuente). Hongo = Colonizar la sala (3 matas al azar en cada sala de combate, radio 1,2, ralentizan 40 % y 1 de daño/s como el micelio; duran hasta limpiar la sala).
- **Toques:** Tiburón = Sangre en el agua (rematar recarga la esquiva). Calamar = Presa a ciegas (cegados cuentan como heridos para Rematar). Tortuga = Retraerse (usar la Vacuola da 1,5 s de invulnerabilidad). Medusa = Estela urticante (esquivar deja rastro que envenena). Escorpión = Picadura oculta (ataque desde camuflaje aplica triple veneno). Moho = Colonia grande (5 matas en vez de 3, 30 % más grandes). Cuesco de lobo = Bocanada al golpe (al recibir golpe, nube cegadora alrededor, máx. cada 6 s).

**Why:** cierra el pendiente del GDD "qué beneficio da cada clase"; `LineageData.Apply/Remove` estaban vacíos. Ver [[primordia-floors-design]].
**How to apply:** escribirlo en el GDD y programarlo siguiendo [[primordia-workflow]]; no repetir mecánicas de evoluciones existentes (Placas, Red micelial).

**Cambios y estado (2026-10-15, en main: commit c118645, merge 33bcb1d):** el usuario pidió Rematar al 50 % de vida (no 30 %) porque piensa subir +50-75 % la vida de los enemigos del piso 1 (anotado en PLAN, feature/balance). Daño con decimales: redondeo al azar (`Health.ScaleDamage`). Código: `Health` (modificadores de daño estáticos, `Barrier`, `MakeInvulnerable`), componentes en `Scripts/Mutations/Lineages/` (Finisher, Grazing, Toxin, Colony, Retract, StingingTrail, HiddenSting, PuffOnHit, `LineageBenefitSettings`), `PlayerLineage` los pone/quita, `LineageData.touch/touchName`, UI `LineageBanner` (HUD) y `LineagePanel` (fila al final del panel de estadísticas de la pausa, cuadro de información al pasar el ratón; pedido 2026-10-15 para descargar la pausa; el cuadro de ranura muestra solo el nivel actual), corazón verde en `HeartsDisplay`. Arte en `docs/multimedia-temp/lineages/generar_linajes.py` (mata = anillo de hadas). Menús: Primordia > Art > Importar beneficios de linajes (llama a Crear linajes).
