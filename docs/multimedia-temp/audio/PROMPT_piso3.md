# Encargo: música y efectos de sonido de Primordia (piso 3, pantano carbonífero)

Tercer encargo de audio, con el mismo sistema, formatos y reglas que los del piso 1 (`PROMPT.md`, «Formato y entrega»)
y el piso 2 (`PROMPT_piso2.md`). Se genera con `generar_audio.py`, desde `efectos_piso3.py` (y más adelante
`musica_piso3.py`). Este encargo empieza con el ambiente del pantano, que hace falta para la sala de paso del piso 2
al 3; el resto del audio del piso (música, Bocas, enemigos, reacciones y jefes) se añade en el paso 8 del PLAN.

## Estilo sonoro (lo decidido hasta ahora)

- **Aire caliente y húmedo, vida por todas partes**, frente al mar abierto del piso 2: anfibios que croan a lo
  lejos, insectos que pasan, lodo que burbujea y gotas que caen del musgo. Más adelante, fuego, metano y veneno
  (reacciones del piso).
- **Tonalidad común del juego: re.** El fondo del ambiente respira sobre **re y la** (73,4 / 110 / 36,7 Hz), con un
  batido lento que lo hace húmedo y cerrado. En el fundido de la sala de paso no choca con el mi del piso 2.
- **Tres tramos del incendio que avanza** (pantano verde, bosque humeante, tierra quemada): de momento solo hay
  ambiente del pantano verde; los de los otros dos tramos van con el resto del audio del piso.

## Ambiente

| Archivo | Uso | Duración | Notas |
|---------|-----|----------|-------|
| `SFX_Amb_Floor3_Loop.wav` | Sube junto a la charca de la sala de paso del piso 2 (fundido por distancia) y suena debajo de la música en el pantano verde (bucle, muy bajo) | 24 s | Aire pesado que respira, fondo en re y la, croar lejano en grupos de 2-3 (graves, re y la), insectos que pasan de un lado a otro, burbujas gordas de lodo y metano con un «blop» grave, gotas del musgo y frondas mecidas. Sin ritmo |
