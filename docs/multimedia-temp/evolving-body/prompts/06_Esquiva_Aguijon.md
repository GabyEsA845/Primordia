# 6. Esquiva del rasgo del Aguijón

---

Te adjunto `generar_cuerpo_evolutivo.py`, el generador de pixel art del cuerpo evolutivo de mi juego *Primordia*, un roguelike con vista cenital en 3/4 que transcurre bajo el agua. El cuerpo del jugador toma distintas formas, y cada mutación pone encima su rasgo en un anclaje. El rasgo `Stinger` (el abdomen con aguijón, en el anclaje `abdomen`) ya tiene `Idle`, `Attack` y `Death` en sus 3 niveles (`stinger_anims`).

Necesito que **añadas al rasgo `Stinger` la animación `Dodge_<dir>`** (`side`, `down` y `up`), en sus 3 niveles:
- **Qué hace:** durante la esquiva, el cuerpo del jugador rueda encogido (son las hojas `Dodge` del cuerpo: 5 cuadros a 20 fps, unos 0,25 s). El abdomen tiene que **enroscarse pegado al cuerpo** y girar con él, sincronizado cuadro a cuadro con esa rueda.
  - **Cuadros 0 y 4:** casi en reposo.
  - **Cuadros 1 a 3:** el abdomen va recogido y la púa queda escondida hacia dentro, para que no parezca que ataca.
- **Niveles:** cada nivel conserva su tamaño y su aspecto (en el 3, la vesícula de veneno sigue brillando).
- **Lienzo y posición:** 32×32, con el centro sobre el anclaje `abdomen` de cada cuadro de la esquiva del cuerpo. Los anclajes de la esquiva ya están en `anclajes.json`, en las hojas `Dodge` de cada forma. Si al girar el abdomen pasa por delante del cuerpo en algún cuadro, márcalo como hace `Attack` con `layer: "front"`, en las claves `Trait_Stinger_<N>_Dodge_<dir>` de `anclajes.json`.
- **Nombre:** `Trait_Stinger_<N>_Dodge_<dir>.png`. Añade también `Dodge` a `MS` (50 ms) si hace falta.

**Vista previa:** añade a cada fila de la vista previa la esquiva en las 3 direcciones, montada con los anclajes de la esquiva del cuerpo.

Reglas:
- Respeta el estilo, el contorno `#181425` y la luz arriba a la izquierda del Aguijón que ya existe.
- **No me devuelvas el script entero.** Dame solo los bloques nuevos o cambiados, cada uno con una línea que diga dónde va (por ejemplo, "sustituir la función `stinger_anims`").
- No generes imágenes, vistas previas ni GIF, y no ejecutes el script: lo ejecuto yo.
