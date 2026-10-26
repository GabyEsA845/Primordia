---
name: primordia-biomass-shop
description: "Decisiones aprobadas (2026-10-13) de biomasa y tienda en Primordia; dos ramas, feature/biomass primero; encargo de arte de la tienda en docs/multimedia-temp/shop/PROMPT.md"
metadata:
  node_type: memory
  type: project
  originSessionId: 6e8d0aad-02e8-4a2d-a229-65e7973ea644
  modified: 2026-10-13T20:59:20.020Z
---

Decisiones del usuario (2026-10-13), ya pasadas al GDD (tabla de decisiones, filas Biomasa y Tienda) en la rama `feature/biomass` (sin commit):

**Biomasa** (arte completo en `docs/multimedia-temp/ui/`: Item_Biomass_Idle 4×10×10, Big_Idle 4×14×14, Collect 4×16×16, HUD/UI_Biomass 12×12; aún sin importar):
- Por enemigo al morir: 10 % grande (vale 5), 40 % pequeña (vale 1) (bajado de 50 % el 2026-10-13); crías solo pequeña al 10 %; jefe 10 fijos y sus invocaciones nada (`Enemy.DropsBiomass`, lo pone BossAI). Mínimo 2 de biomasa por sala de combate.
- Se recoge al tocarla con imán pequeño; al limpiar la sala toda la restante vuela al jugador y se recoge sola (como Enter the Gungeon).
- Contador en el HUD bajo las ranuras de mutación (el minimapa ocupa arriba a la derecha). Se pierde al morir.

**Tienda** (rama `feature/shop`, después; esperando arte pedido en `docs/multimedia-temp/shop/PROMPT.md`):
- 1 por piso, sala lateral sin salida y sin enemigos, icono `UI_Map_Shop` en el mapa.
- Tendero: pequeño Charles Darwin, solo decorativo, con animación al comprar (Idle + Buy), detrás de un mostrador.
- 4 pedestales: 2 mutaciones (15), mejora de nivel de una mutación equipada concreta con su icono e "I → II" (20; usar `PlayerMutations.LevelUp`), medio corazón (5) que pasa a corazón extra (25) si entra con vida llena.
- Compra: acercarse muestra tecla E sobre el artículo; pulsar E compra. Sin dinero: precio rojo y el artículo tiembla.
- Volver a tirar y descuentos por linaje → Mejoras a futuro de PLAN.md (ya añadido).

Ver [[primordia-workflow]], [[primordia-mutation-levels]], [[primordia-multimedia-temp]].

**Implementación de la biomasa (2026-10-13, rama `feature/biomass`, sin commit; esperando prueba del usuario):** `Scripts/Biomass/` (namespace `Primordia.Biomass`): `PlayerBiomass` (Amount, Changed(cantidad, cambio), Add, CanAfford, TrySpend → la tienda lo usará), `BiomassPickup` (sin físicas, por distancia: Pop al aparecer, imán 1,5, Attract, Collect), `RoomBiomass` en cada sala salvo la de inicio (lo añade FloorBuilder si `FloorConfig.SmallBiomass`; Roll/Split estáticos; mínimo lo completa el último enemigo; al Cleared atrae todo tras 0,4 s). `Room.EnemyDied` nuevo evento. `EnemyData` con smallBiomassChance/bigBiomassChance/fixedBiomass. `UI/BiomassCounter` en GameUI (8, -50). Editor: menú **Primordia > Art > Importar biomasa** (`ArtSetup.Biomass.cs`) + `UISetup.EnsureBiomassCounter`. Pruebas en `BiomassTests`.

**Tienda, decisiones extra (2026-10-13):** sala lateral ADICIONAL (aparte de las de combate), cuelga de una sala de combate de la segunda mitad del camino, plantilla Small 22×15. Sin mejora posible → tercera mutación. Mejora a II cuesta 20, a III 30. Mercancía se elige al entrar por primera vez y queda fija (curación vs corazón extra se decide entonces). Cartelito al acercarse: nombre, ranura, "Reemplaza: X". Mutaciones pueden coincidir con otras ofertas (sin las equipadas). Icono en el mapa cuando la sala se descubre (vecina de una visitada y limpia). Acción nueva "Interact" (E, y mando) genérica. Arte entregado en docs/multimedia-temp/shop/ (README con pivotes y tiempos). Arreglado en feature/biomass: MutationPickup equipa en Update, no dentro del trigger (DestroyImmediate en callback de físicas). Orden: el usuario prueba biomasa → commit+merge → feature/shop desde main.

**Biomasa fusionada en main** (2026-10-13, commit cbbcca8, merge 7a506ad).

**Tienda implementada (2026-10-13, rama `feature/shop`, sin commit; esperando prueba):** `Core/Interactable` (registro estático Active, radio, SetFocused/Interact) + `Player/PlayerInteractor` (acción nueva `Player/Interact`: E y Gamepad buttonWest en InputSystem_Actions). `Scripts/Shop/` (namespace `Primordia.Shop`): `ShopPedestal` (Interactable; artículo flotante, brillo, tecla E, precio/nivel en lienzo del mundo, Shake, Sell) y `ShopRoom` (Stock al primer Entered, PriceOf, TryBuy, Describe, Darwin estados `Darwin_Idle`/`Darwin_Buy`, MoveCounter si hay puerta arriba). `UI/ShopTooltip` en GameUI abajo-centro. `RoomRole.Shop`, `FloorLayoutGenerator.ShopCell`, `FloorConfig.ShopTemplate/ShopPrefab/HasShop/ConfigureShop`, FloorBuilder instancia el prefab en el centro del suelo. `MapUI.SetShopIcon/ShopDiscovered`. Menú **Primordia > Art > Importar tienda** (`ArtSetup.Shop.cs`). Pruebas en `ShopTests`.

**Pedestales dinámicos programados (2026-10-13, feature/shop, sin commit; esperando prueba):** cada pedestal de mutación es una oferta por ESA mutación (`ShopRoom.OfferOf`, `ShopPedestal.Offer/SetOffer`): no la lleva → mutación 15; la lleva y puede subir → mejora 20/30 (aunque la consiguiera o subiera fuera); en III → `Close` definitivo. Cada pedestal se compra una vez. Señales: comprado → `Shop_Unavailable` (círculo con barra, flota con sombra); no disponible → `Shop_Unavailable_Sign` (letrero con X, clavado, sin flotar ni sombra). Arte en `docs/multimedia-temp/shop-unavailable/`, importado a `Art/Shop/Marks/` por Importar tienda. Pruebas nuevas en ShopTests.

**Tienda fusionada en main** (2026-10-13, commit baed6fc, merge ccba8b0; rama feature/shop subida).
