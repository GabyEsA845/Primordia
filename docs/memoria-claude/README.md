# Memoria de Claude Code para Primordia

Copia de las notas que Claude Code guarda sobre este proyecto (en qué paso va cada rama, las decisiones de diseño, cómo
está hecho cada módulo y las trampas de Unity). Claude Code no las lee desde aquí: las busca en la carpeta de memoria de
su proyecto, en la carpeta del usuario. Esta copia sirve para llevarlas a otro equipo.

`MEMORY.md` es el índice; cada nota es un archivo aparte.

## Restaurarla en otro equipo

1. Clona el repositorio y cambia a la rama en curso (por ejemplo `git switch main`).
2. Abre Claude Code en la carpeta del proyecto y pídele: *"Copia las notas de docs/memoria-claude a tu memoria de este
   proyecto"*. Claude sabe en qué carpeta va su memoria en ese equipo.

   O a mano: copia todos los `.md` de esta carpeta (menos este `README.md`) a
   `%USERPROFILE%\.claude\projects\<proyecto>\memory\`, donde `<proyecto>` es la ruta del proyecto con `\` y `:` cambiados
   por `-` (con el proyecto en `D:\Proyectos\Primordia`, es `d--Proyectos-Primordia`).

## Al volver

Cuando se trabaje desde otro equipo, conviene volver a copiar aquí la memoria antes de subir los cambios, para que el
siguiente equipo tenga la versión al día.
