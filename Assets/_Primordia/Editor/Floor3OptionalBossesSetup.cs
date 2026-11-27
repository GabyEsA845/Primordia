using System.Linq;
using Primordia.Enemies;
using Primordia.Rooms;
using UnityEditor;
using UnityEngine;

namespace Primordia.Editor
{
    /// <summary>
    /// Engancha al piso 3 los jefes opcionales ya creados: el Nido de la Megarachne (lateral del Bosque humeante que sale a veces) y
    /// el Lepidodendron detrás del muro de raíces de la antesala secreta, que pasa también al Bosque humeante.
    /// </summary>
    public static class Floor3OptionalBossesSetup
    {
        public const float NestChance = 0.33f;
        // El Bosque humeante es la segunda franja.
        const int SmokeBand = 1;
        const string AntechamberName = "Room_Special_SecretAntechamber";
        const string OldChamberPath = "Assets/_Primordia/Data/Rooms/Floor3/Room_Special_SecretChamber.asset";

        [MenuItem("Primordia/Floor 3/Enganchar jefes opcionales")]
        internal static void RegisterMenu()
        {
            var config = AssetDatabase.LoadAssetAtPath<FloorConfig>(Floor3Setup.Floor3ConfigPath);
            if (config == null || !Register(config))
                return;
            AssetDatabase.DeleteAsset(OldChamberPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Floor3OptionalBossesSetup: Nido de la Megarachne y jefe secreto enganchados en Floor_3.");
        }

        /// <summary>Pone en las especiales del piso el Nido y el jefe secreto. False (sin tocar nada) si falta algo por crear.</summary>
        internal static bool Register(FloorConfig config)
        {
            var nestRoom = AssetDatabase.LoadAssetAtPath<RoomTemplate>(MegarachneBossSetup.RoomPath);
            var nestBoss = AssetDatabase.LoadAssetAtPath<Enemy>(MegarachneBossSetup.PrefabPath);
            var arena = AssetDatabase.LoadAssetAtPath<RoomTemplate>(LepidodendronBossSetup.RoomPath);
            var secretBoss = AssetDatabase.LoadAssetAtPath<Enemy>(LepidodendronBossSetup.PrefabPath);
            var rooms = config.SpecialRooms.ToList();
            int antechamber = rooms.FindIndex(room => room.template != null && room.template.name == AntechamberName);
            if (nestRoom == null || nestBoss == null || arena == null || secretBoss == null || antechamber < 0)
            {
                Debug.LogError("Floor3OptionalBossesSetup: faltan los jefes (Primordia > Boss) o la antesala secreta "
                    + "(Primordia > Floor 3 > Crear salas especiales).");
                return false;
            }

            var secret = rooms[antechamber];
            secret.always = true;
            secret.band = SmokeBand;
            secret.secretArena = arena;
            secret.secretBoss = secretBoss;
            rooms[antechamber] = secret;

            // La cámara provisional de antes y un Nido ya registrado se sustituyen.
            rooms.RemoveAll(room => room.template == null || room.template == nestRoom);
            rooms.Add(new FloorConfig.SpecialRoom
            {
                template = nestRoom, placement = FloorConfig.SpecialPlacement.Side, band = SmokeBand,
                chance = NestChance, lairBoss = nestBoss,
            });
            config.ConfigureSpecialRooms(rooms.ToArray(), config.SpecialRoomCount);
            EditorUtility.SetDirty(config);
            return true;
        }
    }
}
