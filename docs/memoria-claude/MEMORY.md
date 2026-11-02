## Cómo trabajar
- [Ahorro de tokens](primordia-token-saving.md) — una cuenta Pro; AVISAR SIEMPRE cambio Opus (diseño/arte) ↔ Sonnet (implementar) al empezar y al acabar cada tarea; agrupar arte/audio
- [Flujo de trabajo](primordia-workflow.md) — español, rama por módulo, menús de editor, commit solo tras confirmar
- [Arte de jefes](primordia-boss-art-workflow.md) — un jefe cada vez, preguntas antes de cada subtanda, muestra en 1 dirección, escenas (GIF en gif/escenas/), comparar versiones si duda; PRESENTACIÓN; nada que parezca «pegado sobre el fondo»
- [Trampas de Unity](primordia-unity-gotchas.md) — Unity abierto, logs, statics, CRLF, zips, referencias perdidas
- [Multimedia temporal](primordia-multimedia-temp.md) — arte/audio/generadores en docs/multimedia-temp/, se importan con menús Primordia
- [Skill primordia-sprites](primordia-sprites-skill.md) — usarla en todo arte nuevo; PREGUNTAR SIEMPRE antes de cualquier sprite o animación
- [Skill primordia-audio](primordia-audio-skill.md) — usarla en todo audio nuevo
- [Mutaciones por niveles](primordia-mutation-visual-tiers.md) — normales = efectos sin tocar silueta; pieles retocan dentro; especiales cambian el modelo
- [Documentos por piso](primordia-floor-docs.md) — actualizar el documento interactivo del piso al terminar cada cosa
- [Arte pendiente (tanda Opus)](primordia-art-pending-batch.md) — lista de arte provisional por sustituir: corazón de sala, iconos de mapa, regreso
- [Icono de corazón en el mapa](primordia-room-heart-map-icon.md) — pendiente: sprite definitivo del corazón de sala + icono de mapa para salas con corazón sin recoger
- [Codex aparcado](primordia-codex-paused.md) — trabajar con Codex está archivado; no proponerlo

- [Tanda de arte del piso 3](primordia-floor3-art-batch-pending.md) — lista acumulada (lodo en cuerpo, puertas, llamas, iconos, efecto del agua) + código ligado
- [Pulido: jugador en las puertas](primordia-door-crossing-polish.md) — pendiente: el jugador se ve «pegado» al cruzar el arco (todos los pisos)

- [Pulido: lodo en el cuerpo](primordia-mud-body-polish.md) — pendiente: grumos/burbujas y lodo que se limpia en vez de la máscara recta (con las puertas)

## En curso
- [Estado de Primordia](primordia-project-status.md) — para itch.io; pisos 1 y 2 en main; piso 3 EN CURSO en feature/floor-3 (pasos 1 estructura y 2 reacciones HECHOS, sin push); SIGUIENTE paso 3 Cuerpos y Bocas
- [Reacciones del piso 3](primordia-floor3-reactions-decisions.md) — paso 2 HECHO (subtandas A-D): quemado tipo Gungeon, odre, veneno, fumarolas
- [Estructura del piso 3](primordia-floor3-structure-decisions.md) — subtandas, decisiones del generador, estado
- [Diseño del piso 3](primordia-floor3-design.md) — CERRADO; arte COMPLETO en feature/floor3-art (tandas 0-6, 4 jefes); luego documento, GDD/PLAN, ramas (Sonnet)
- [Jefe Meganeura](primordia-boss-meganeura.md) — combate y arte terminados (2026-11-01), generadores y reglas para el código
- [Minijefe Megarachne](primordia-boss-megarachne.md) — TERMINADA (2026-11-01): combate, árbol del Nido, cables, reglas para el código
- [Jefes con mecánicas](primordia-boss-mechanics.md) — desde el piso 3 los jefes usan terreno/reacciones; escenario más vivo

## Diseño general (consulta)
- [Diseño de pisos](primordia-floors-design.md) — 6 pisos, criatura por piso × familia de arma; v1.0 = pisos 1-3
- [Progresión por pisos](primordia-floor-progression.md) — mutaciones/linajes por piso, sala de paso, Boca, metamorfosis
- [Niveles de mutaciones](primordia-mutation-levels.md) — 3 niveles y evoluciones
- [Beneficios de linajes](primordia-lineage-benefits.md) — base por clase + toque por animal
- [Evolución de linajes](primordia-lineage-evolution.md) — animación de 2 s + piezas por linaje
- [Cuerpo evolutivo](primordia-evolving-body.md) — DESCARTADO, no integrar

## Terminado (detalles de implementación)
- [Jefe secreto Lepidodendron](primordia-boss-lepidodendron.md) — TERMINADO (commit bf0e8bf): resumen para importarlo e implementarlo
- [Sprites: estado](primordia-sprites-next.md) — personajes, HUD, menús, minimapa, tienda
- [Biomasa y tienda](primordia-biomass-shop.md)
- [Esporas y Aguijón](primordia-spores-stinger.md)
- [Audio del piso 1](primordia-audio.md)
- [Ameba gigante](primordia-boss-amoeba.md) — jefe 2 del piso 1
- [Diseño del piso 2](primordia-floor2-design.md)
- [Salas del piso 2](primordia-floor2-rooms.md) — corazón morado → secreto del piso 3
- [Estructura del piso 2](primordia-floor2-structure.md) — atajos F1–F4
- [Mutaciones de las almejas](primordia-clam-mutations.md)
- [Bocas del piso 2](primordia-floor2-mouths.md)
- [Enemigos del piso 2](primordia-floor2-enemies.md) — EnemyBrain, tope por sala
- [Sala de cambio](primordia-exchange-room.md)
- [Mutaciones del piso 2](primordia-floor2-mutations.md) — 8 linajes, Carroña, F8
- [Jefe Anomalocaris](primordia-boss-anomalocaris.md) — BossBarSkin/IPhasedBoss
- [Jefe Euriptérido](primordia-boss-eurypterid.md) — derrumbe, bullet hell
- [Audio del piso 2](primordia-floor2-audio.md) — Sfx.Stop
- [Cambios pisos 1-2](primordia-pending-floor12-changes.md) — 5 cambios pedidos (velocidad parkour, corazón verde, morado, salas piso 1, tope veneno jefes)
