#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Primordia.Enemies;
using Primordia.Mutations;
using Primordia.Rooms;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Core
{
    /// <summary>
    /// Atajos para probar (solo en el editor y en compilaciones de desarrollo; la build de itch.io no los lleva):
    /// F1, F2 y F3 rotan por las Extremidades, la Piel y los Órganos del piso (con el mismo nivel), F4 completa el siguiente
    /// linaje del piso (equipa sus 4 mutaciones, para probar beneficios, toques y evoluciones), F5 sube un nivel la primera
    /// mutación que pueda (Boca, Extremidades, Piel, Órgano), F6 cambia a la siguiente Boca del piso, F7 invulnerable y
    /// F8 lleva junto a la cápsula de la sala de cambio. Con Mayús: F2 daño ×5, F3 vacía la sala en combate, F4 da una
    /// mutación al azar de la reserva del piso (o sube una que ya se lleva) y F5 una especial de las almejas. F9 derrumba las rocas agrietadas de la sala y F10 pasa el derrumbe de la cueva a su
    /// siguiente fase (calma, crujidos, terremoto, derrumbe; sala del Euriptérido), Av Pág / Re Pág saltan al tramo siguiente o
    /// anterior del piso (piso 3) y F12 prende lo inflamable cercano (maleza y helechos del piso 3; con Mayús, un fuego en el suelo). Arriba a la izquierda se ve qué está activo.
    /// Solo editor, Lepidodendron: Mayús+F8 recorre pared 1 → pared 2 → pared 3 → arena;
    /// Mayús+F9 sacude la copa durante 1 s.
    /// </summary>
    public class DebugCheats : MonoBehaviour
    {
        const float DamageBoost = 5f;
        const int KillDamage = 100000;

        static DebugCheats instance;

        bool invulnerable;
        bool strong;
        Room current;
        PlayerMutations player;
        string message;
        float messageUntil;
#if UNITY_EDITOR
        int lepidoStop;
        LepidodendronBoss lepidoTestBoss;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (instance != null)
                return;
            var cheats = new GameObject("DebugCheats");
            DontDestroyOnLoad(cheats);
            instance = cheats.AddComponent<DebugCheats>();
        }

        void OnEnable() => Room.PlayerEnteredAny += OnRoomEntered;

        void OnDisable() => Room.PlayerEnteredAny -= OnRoomEntered;

        void OnRoomEntered(Room room) => current = room;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (player == null)
                player = FindAnyObjectByType<PlayerMutations>();
            if (player == null)
                return;

            bool shift = keyboard.leftShiftKey.isPressed;
            // Sin Mayús: F1-F3 rotan Extremidades, Piel y Órganos del piso; F4 rota los linajes; F5 sube de nivel; F7 invulnerable.
            // Con Mayús: lo que antes estaba en F2-F5 (daño ×5, vaciar sala, mutación al azar, especial).
            if (keyboard.f1Key.wasPressedThisFrame && !shift)
                NextSlotMutation(MutationSlot.Limbs, "Extremidades");
            // Mayús+F1: un medio corazón de sala en el suelo, delante (para probar el almacén y la cucaracha ladrona).
            if (keyboard.f1Key.wasPressedThisFrame && shift)
                DropRoomHeart();
            if (keyboard.f2Key.wasPressedThisFrame)
            {
                if (shift)
                {
                    strong = !strong;
                    if (!strong && player.Weapon != null)
                        player.Weapon.ClearDamageBonus(this);
                    Say(strong ? $"Daño ×{DamageBoost}" : "Daño normal");
                }
                else
                    NextSlotMutation(MutationSlot.Skin, "Piel");
            }
            if (keyboard.f3Key.wasPressedThisFrame)
            {
                if (shift)
                    ClearRoom();
                else
                    NextSlotMutation(MutationSlot.Organ, "Órgano");
            }
            if (keyboard.f4Key.wasPressedThisFrame)
            {
                if (shift)
                    GiveMutation();
                else
                    NextLineage();
            }
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                if (shift)
                    GiveSpecial();
                else
                    LevelUpNext();
            }
            if (keyboard.f6Key.wasPressedThisFrame)
                NextMouth();
            if (keyboard.f7Key.wasPressedThisFrame)
                Say((invulnerable = !invulnerable) ? "Invulnerable" : "Vulnerable");
            if (keyboard.f8Key.wasPressedThisFrame && !shift)
                GoToExchange();
            if (keyboard.f9Key.wasPressedThisFrame && !shift)
                CollapseRocks();
#if UNITY_EDITOR
            if (keyboard.f8Key.wasPressedThisFrame && shift)
                LepidodendronTest(false);
            if (keyboard.f9Key.wasPressedThisFrame && shift)
                LepidodendronTest(true);
#endif
            if (keyboard.f11Key.wasPressedThisFrame)
                CycleTeleport();
            if (keyboard.f10Key.wasPressedThisFrame)
                NextCaveMode();
            if (keyboard.f12Key.wasPressedThisFrame)
            {
                // Con Mayús, un fuego en el suelo un par de tiles por delante; sin ella, prende lo inflamable cercano.
                if (keyboard.leftShiftKey.isPressed)
                    SpawnFire();
                else
                    IgniteNearby();
            }
            if (keyboard.pageDownKey.wasPressedThisFrame)
                GoToBand(1);
            if (keyboard.pageUpKey.wasPressedThisFrame)
                GoToBand(-1);

            // La esquiva enciende y apaga la invulnerabilidad: aquí se renueva cada cuadro sin tocarla.
            if (invulnerable && player.Health != null)
                player.Health.MakeInvulnerable(0.2f);
            if (strong && player.Weapon != null)
                player.Weapon.SetDamageBonus(this, DamageBoost);
        }

#if UNITY_EDITOR
        void LepidodendronTest(bool shake)
        {
            var bosses = FindObjectsByType<LepidodendronBoss>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (bosses.Length == 0)
            {
                Say("No hay Lepidodendron en este piso");
                return;
            }
            var boss = bosses[0];
            if (lepidoTestBoss != boss)
            {
                lepidoTestBoss = boss;
                lepidoStop = 0;
            }
            if (shake)
            {
                boss.ShakeCanopy(1f);
                Say("Copa: sacudida de 1 s");
                return;
            }
            var room = boss.GetComponentInParent<Room>();
            if (room != null)
                room.Begin();
            var position = boss.TestStop(lepidoStop);
            if (player.TryGetComponent<Rigidbody2D>(out var body))
            {
                body.linearVelocity = Vector2.zero;
                body.position = position;
            }
            player.transform.position = position;
            Say(lepidoStop < 3 ? $"Lepidodendron: delante de pared {lepidoStop + 1}" : "Lepidodendron: arena");
            lepidoStop = (lepidoStop + 1) % 4;
        }
#endif

        /// <summary>Mayús+F1: deja un medio corazón de sala unos tiles por delante, en la sala en que estás.</summary>
        void DropRoomHeart()
        {
            var hearts = current != null ? current.GetComponent<RoomHearts>() : null;
            if (hearts == null)
            {
                Say("Esta sala no suelta corazones");
                return;
            }
            Vector2 aim = player.Controller != null ? player.Controller.AimDirection : Vector2.right;
            hearts.DebugDrop((Vector2)player.transform.position + aim.normalized * 2.5f);
            Say("Medio corazón en el suelo");
        }

        /// <summary>F11: viaja al siguiente teletransporte activo; si no hay otro, vuelve a la marca de regreso.</summary>
        void CycleTeleport()
        {
            if (!TeleportNetwork.CanTravel())
            {
                Say("Ahora no se puede teletransportar (sala sin limpiar)");
                return;
            }
            if (TeleportNetwork.HasReturn)
            {
                TeleportNetwork.TravelBack();
                Say("Regreso");
                return;
            }
            var destinations = TeleportNetwork.Destinations();
            if (destinations.Length == 0)
            {
                Say("No hay teletransportes activos");
                return;
            }
            var next = destinations[teleportIndex++ % destinations.Length];
            TeleportNetwork.TravelTo(next);
            Say($"Teletransporte: {next.Role}");
        }

        int teleportIndex;

        /// <summary>Mayús + F12: enciende un fuego en el suelo 2 u a la derecha del jugador.</summary>
        void SpawnFire()
        {
            var position = (Vector2)player.transform.position + Vector2.right * 2f + Vector2.down * 0.3f;
            Say(GroundFire.Spawn(position) != null ? "Fuego en el suelo" : "No hay arte del fuego (ReactionKit)");
        }

        /// <summary>F12: prende lo inflamable (la maleza quemable) a menos de 6 u del jugador.</summary>
        void IgniteNearby()
        {
            int count = 0;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>())
            {
                if (behaviour is IFlammable flammable && flammable.CanIgnite
                    && ((Vector2)behaviour.transform.position - (Vector2)player.transform.position).sqrMagnitude <= 36f)
                {
                    flammable.Ignite();
                    count++;
                }
            }
            Say(count > 0 ? $"Se prende {count} inflamable(s)" : "Nada inflamable cerca");
        }

        /// <summary>Av Pág / Re Pág: salta a la sala más cercana del tramo (franja) siguiente o anterior del piso.</summary>
        void GoToBand(int step)
        {
            int band = current != null ? current.DepthBand : -1;
            if (band < 0)
            {
                Say("Este piso no tiene tramos o no estás en una sala con tramo");
                return;
            }
            Room target = null;
            float best = float.MaxValue;
            foreach (var room in FindObjectsByType<Room>())
            {
                if (room.DepthBand != band + step || !room.TryGetComponent<Collider2D>(out var area))
                    continue;
                float distance = ((Vector2)area.bounds.center - (Vector2)player.transform.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    target = room;
                }
            }
            if (target == null)
            {
                Say("No hay más tramos en esa dirección");
                return;
            }
            Vector2 position = target.GetComponent<Collider2D>().bounds.center;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
            player.transform.position = position;
            Say($"Tramo {band + step + 1}");
        }

        void GoToExchange()
        {
            var capsule = FindAnyObjectByType<ExchangeCapsule>();
            if (capsule == null)
            {
                Say("Este piso no tiene sala de cambio");
                return;
            }
            // Delante de la cápsula, por debajo de su bloque de 2×2.
            var position = (Vector2)capsule.transform.position + Vector2.down * 2.5f;
            if (player.TryGetComponent<Rigidbody2D>(out var body))
                body.position = position;
            player.transform.position = position;
            Say("Sala de cambio");
        }

        void CollapseRocks()
        {
            int count = CollapsingRock.CollapseAll(current);
            Say(count > 0 ? $"Se derrumban {count} rocas" : "No hay rocas que derrumbar en esta sala");
        }

        void NextCaveMode()
        {
            var cave = current != null ? current.GetComponent<CaveCollapse>() : null;
            if (cave == null)
            {
                Say("Esta sala no tiene derrumbe");
                return;
            }
            var next = (CaveCollapse.Mode)(((int)cave.Current + 1) % 4);
            cave.SetMode(next);
            Say(next switch
            {
                CaveCollapse.Mode.Calm => "Cueva: calma",
                CaveCollapse.Mode.Creaking => "Cueva: crujidos",
                CaveCollapse.Mode.Quake => "Cueva: terremoto",
                _ => $"Cueva: derrumbe ({cave.PendingZones} zonas por hundir)"
            });
        }

        void ClearRoom()
        {
            if (current == null || current.State != Room.RoomState.Combat)
            {
                Say("No hay combate en esta sala");
                return;
            }
            foreach (var enemy in current.GetComponentsInChildren<Enemy>())
            {
                if (enemy.Health != null && !enemy.Health.IsDead)
                    enemy.Health.TakeDamage(KillDamage);
            }
            Say("Sala vaciada");
        }

        void GiveMutation()
        {
            var generator = FindAnyObjectByType<FloorGenerator>();
            var pool = generator != null && generator.Config != null ? generator.Config.RewardPool : null;
            var offer = pool != null ? pool.PickOffer(1, player) : null;
            if (offer == null || offer.Count == 0)
            {
                Say("No quedan mutaciones que dar");
                return;
            }

            var mutation = offer[0];
            if (player.IsEquipped(mutation))
            {
                player.LevelUp(mutation.Slot);
                Say($"{mutation.name} sube a nivel {player.LevelOf(mutation.Slot)}");
            }
            else
            {
                player.Equip(mutation);
                Say($"{mutation.name} equipada");
            }
        }

        void GiveSpecial()
        {
            var generator = FindAnyObjectByType<FloorGenerator>();
            var specials = generator != null && generator.Config != null ? generator.Config.Clams.bigMutations : null;
            var free = new System.Collections.Generic.List<MutationData>();
            var upgradable = new System.Collections.Generic.List<MutationData>();
            foreach (var special in specials ?? System.Array.Empty<MutationData>())
            {
                if (special == null)
                    continue;
                if (!player.IsEquipped(special))
                    free.Add(special);
                else if (player.CanLevelUp(special))
                    upgradable.Add(special);
            }

            var list = free.Count > 0 ? free : upgradable;
            if (list.Count == 0)
            {
                Say("No quedan especiales que dar en este piso");
                return;
            }
            var mutation = list[Random.Range(0, list.Count)];
            player.Equip(mutation);
            Say(free.Count > 0 ? $"{mutation.name} equipada (especial)" : $"{mutation.name} sube a nivel {player.LevelOf(mutation.Slot)}");
        }

        /// <summary>Cambia a la siguiente Boca del piso actual (en orden de nombre) y la deja al nivel de la que se llevaba.</summary>
        void NextMouth()
        {
            var mouths = FloorMouths(Mathf.Max(1, FloorGenerator.CurrentFloor));
            if (mouths.Count == 0)
            {
                Say("Este piso no tiene Bocas");
                return;
            }

            var current = player.Get(MutationSlot.Mouth);
            int level = Mathf.Max(1, player.LevelOf(MutationSlot.Mouth));
            var next = mouths[(mouths.IndexOf(current) + 1) % mouths.Count];
            if (current != null)
                player.Unequip(MutationSlot.Mouth);
            player.Equip(next);
            while (player.LevelOf(MutationSlot.Mouth) < level && player.LevelUp(MutationSlot.Mouth))
            {
            }
            Say($"Boca: {next.DisplayName} (nivel {player.LevelOf(MutationSlot.Mouth)})");
        }

        /// <summary>
        /// F1-F3: cambia la mutación de esa ranura por la siguiente del piso actual (en orden de nombre, sin las especiales) y la
        /// deja al nivel de la anterior.
        /// </summary>
        void NextSlotMutation(MutationSlot slot, string slotName)
        {
            var all = AllAssets<MutationData>();
            all.RemoveAll(m => m.Slot != slot || m.Floor != Mathf.Max(1, FloorGenerator.CurrentFloor) || m.IsSpecial);
            if (all.Count == 0)
            {
                Say($"Este piso no tiene mutaciones de {slotName}");
                return;
            }

            var current = player.Get(slot);
            int level = Mathf.Max(1, player.LevelOf(slot));
            var next = all[(all.IndexOf(current) + 1) % all.Count];
            if (current != null)
                player.Unequip(slot);
            player.Equip(next);
            while (player.LevelOf(slot) < level && player.LevelUp(slot))
            {
            }
            Say($"{slotName}: {next.DisplayName} (nivel {player.LevelOf(slot)})");
        }

        static readonly MutationSlot[] LevelOrder = { MutationSlot.Mouth, MutationSlot.Limbs, MutationSlot.Skin, MutationSlot.Organ };

        /// <summary>F5: sube un nivel la primera mutación equipada que aún pueda subir, en el orden Boca, Extremidades, Piel y Órgano.</summary>
        void LevelUpNext()
        {
            foreach (var slot in LevelOrder)
            {
                var mutation = player.Get(slot);
                if (mutation == null || !player.CanLevelUp(mutation))
                    continue;
                player.LevelUp(slot);
                Say($"{mutation.DisplayName} sube a nivel {player.LevelOf(slot)}");
                return;
            }
            Say("Todo está al nivel máximo");
        }

        /// <summary>
        /// Completa el siguiente linaje del piso actual (en orden de nombre, después del que está activo): cambia cada ranura
        /// por la mutación del linaje, a nivel I. La evolución salta como al completarlo jugando.
        /// </summary>
        void NextLineage()
        {
            int floor = Mathf.Max(1, FloorGenerator.CurrentFloor);
            var lineages = AllAssets<LineageData>();
            lineages.RemoveAll(l => l.Floor != floor);
            if (lineages.Count == 0)
            {
                Say("Este piso no tiene linajes");
                return;
            }

            var active = player.TryGetComponent<PlayerLineage>(out var playerLineage) ? playerLineage.Active : null;
            var next = lineages[(lineages.IndexOf(active) + 1) % lineages.Count];
            // Primero se quita lo que sobra y luego se equipa: así no se completa otro linaje a medio camino.
            for (int slot = 0; slot < LineageData.SlotCount; slot++)
            {
                var mutation = next.MutationFor((MutationSlot)slot);
                if (mutation != null && !player.IsEquipped(mutation) && player.Get((MutationSlot)slot) != null)
                    player.Unequip((MutationSlot)slot);
            }
            for (int slot = 0; slot < LineageData.SlotCount; slot++)
            {
                var mutation = next.MutationFor((MutationSlot)slot);
                if (mutation != null && !player.IsEquipped(mutation))
                    player.Equip(mutation);
            }
            Say($"Linaje: {next.DisplayName} ({MutationText.ClassName(next.Class)})");
        }

        /// <summary>Las Bocas de ese piso que no son especiales.</summary>
        static System.Collections.Generic.List<MutationData> FloorMouths(int floor)
        {
            var all = AllAssets<MutationData>();
            all.RemoveAll(m => m.Slot != MutationSlot.Mouth || m.Floor != floor || m.IsSpecial);
            return all;
        }

        /// <summary>En el editor, todos los de ese tipo del proyecto; si no, los cargados. En orden de nombre.</summary>
        static System.Collections.Generic.List<T> AllAssets<T>() where T : Object
        {
#if UNITY_EDITOR
            var all = new System.Collections.Generic.List<T>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                    all.Add(asset);
            }
#else
            var all = new System.Collections.Generic.List<T>(Resources.FindObjectsOfTypeAll<T>());
#endif
            all.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return all;
        }

        void Say(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 2f;
        }

        void OnGUI()
        {
            string state = (invulnerable ? "INVULNERABLE  " : "") + (strong ? $"DAÑO ×{DamageBoost}  " : "");
            string line = Time.unscaledTime < messageUntil ? message : null;
            if (state.Length == 0 && line == null)
                return;
            GUI.Label(new Rect(10, 10, 600, 22), $"[pruebas] {state}{line}");
        }
    }
}
#endif
