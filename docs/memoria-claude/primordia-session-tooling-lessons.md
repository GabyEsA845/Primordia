---
name: primordia-session-tooling-lessons
description: "Trampas de herramientas de la sesión del 2026-11-03: comprobar compilación sin Unity, ediciones con Python, capturas del documento y republicar el artifact"
metadata:
  type: reference
---

- **Compilar sin Unity** (Unity abierto): copiar `Primordia.csproj` a una copia de seguridad, añadirle a mano las líneas `<Compile Include=...>` de los archivos nuevos, compilar `Primordia.Editor.csproj` y `Primordia.Tests.PlayMode.csproj` con copias temporales (que también llevan sus archivos nuevos) con `dotnet build ... -p:BaseIntermediateOutputPath=<scratchpad>/obj/ -o <scratchpad>/out`, y restaurar el csproj. Los csproj están en .gitignore; Unity los regenera. Las pruebas PlayMode solo se ejecutan en Unity.
- **Bash y comillas:** los heredocs largos con comillas y apóstrofos fallan a menudo en esta Bash (`unexpected EOF`). Para scripts de edición grandes, escribir el `.py` con la herramienta Write y ejecutarlo; para C# o HTML, usar Write/Edit. Mantener CRLF al editar con Python (`replace('\r\n','\n')` y volver).
- **Campos serializados de los ScriptableObject:** si se cambia el tipo de un campo de una mutación (número a lista), los assets ya creados lo guardan vacío y salta `IndexOutOfRange`. Renombrar el campo y volver a ejecutar el menú «Crear…».
- **Captura del documento interactivo** (para revisar la maquetación sin abrir el navegador): Chrome headless (`chrome.exe --headless=new --window-size=1300,3400 --virtual-time-budget=6000 --screenshot=...`) sobre una copia del HTML a la que se le añade al final un `<script>` que quita las secciones que no interesan; los GIF salen en su primer cuadro.
- **Republicar un artifact** ya publicado: la primera publicación se rechaza y guarda la versión viva; leer unas líneas de ese archivo, comparar con un diff que sustituya las líneas largas por su hash y publicar otra vez sin cambios; si pide repetir la llamada idéntica, repetirla.
- **Python en Windows:** usar `"$LOCALAPPDATA/Programs/Python/Python312/python.exe"` (el `python` del PATH es el alias de la tienda en algunos casos).
- **Compilar sin Unity, receta de la sesión del paso 5:** copia de seguridad de los tres csproj en el scratchpad, un script Python que añade los `<Compile Include=...>` de los archivos nuevos tras un archivo ancla (con `\r` al final de cada línea nueva), `dotnet build` de `Primordia.Editor.csproj` y `Primordia.Tests.PlayMode.csproj` (el segundo compila también Primordia) y restaurar los csproj. Un error típico: faltan `using` en los archivos del editor.
- **El `Logs/Editor.log` del proyecto no guarda los mensajes de fallo del Test Runner** (solo avisos de compilación): pedir a la usuaria el nombre de la prueba y el texto rojo. Causa típica en mis pruebas: usar `GetComponent` sobre un enemigo que ya murió (`Enemy` se destruye al morir): guardar antes la referencia a `Health`.
- **Ediciones grandes con Python:** escribirlas con la herramienta Write en un `.py` (los heredocs con comillas fallan en esta Bash) y mantener CRLF (`utf-8-sig` al leer, `newline=''` al escribir).
