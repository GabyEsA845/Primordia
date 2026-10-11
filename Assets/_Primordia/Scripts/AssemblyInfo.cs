using System.Runtime.CompilerServices;

// Las pruebas pueden simular la entrada del jugador sin teclado ni ratón,
// y los menús del editor pueden configurar datos que en el juego son de solo lectura.
[assembly: InternalsVisibleTo("Primordia.Tests.PlayMode")]
[assembly: InternalsVisibleTo("Primordia.Editor")]
