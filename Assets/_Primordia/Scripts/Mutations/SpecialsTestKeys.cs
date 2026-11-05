using Primordia.Combat;
using Primordia.Core;
using Primordia.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Primordia.Mutations
{
    /// <summary>
    /// Atajos de la escena de prueba de las mutaciones especiales del ámbar (Test_Floor3Specials). 1/2/3 ponen Cercos, Cápsula o Gota
    /// a nivel I y, al repetir la tecla, suben a II, III y vuelven al I. 4 deja al jugador a medio corazón (para la Cápsula), 5 le cura
    /// del todo, 6 enciende o apaga un tirador de prueba que le dispara proyectiles (para los Cercos) y 7 saca un enemigo delante
    /// (para la Gota). 0 quita la especial. Solo para probar.
    /// </summary>
    public class SpecialsTestKeys : MonoBehaviour
    {
        [SerializeField] MutationData[] specials = System.Array.Empty<MutationData>();
        [SerializeField] AttackData shooterAttack;
        [SerializeField] Enemy dummyEnemy;
        [SerializeField, Min(0.2f)] float shooterEvery = 1.4f;
        [SerializeField, Min(2f)] float shooterDistance = 7f;

        PlayerMutations player;
        Health health;
        MutationData current;
        bool shooting;
        float nextShot;
        string notice = "";
        float noticeUntil;

        void Start()
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found == null)
                return;
            player = found.GetComponent<PlayerMutations>();
            health = found.GetComponent<Health>();
        }

        void Update()
        {
            if (player == null)
            {
                Start();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (Pressed(keyboard.digit1Key, keyboard.numpad1Key))
                Pick(0);
            else if (Pressed(keyboard.digit2Key, keyboard.numpad2Key))
                Pick(1);
            else if (Pressed(keyboard.digit3Key, keyboard.numpad3Key))
                Pick(2);
            else if (Pressed(keyboard.digit0Key, keyboard.numpad0Key))
                Clear();
            else if (Pressed(keyboard.digit4Key, keyboard.numpad4Key))
                HalfHeart();
            else if (Pressed(keyboard.digit5Key, keyboard.numpad5Key))
                FullHealth();
            else if (Pressed(keyboard.digit6Key, keyboard.numpad6Key))
                ToggleShooter();
            else if (Pressed(keyboard.digit7Key, keyboard.numpad7Key))
                SpawnDummy();

            if (shooting && Time.time >= nextShot)
            {
                nextShot = Time.time + shooterEvery;
                Shoot();
            }
        }

        static bool Pressed(params UnityEngine.InputSystem.Controls.KeyControl[] keys)
        {
            foreach (var key in keys)
                if (key.wasPressedThisFrame)
                    return true;
            return false;
        }

        void Pick(int index)
        {
            if (index < 0 || index >= specials.Length || specials[index] == null)
                return;
            var mutation = specials[index];
            int level = 1;
            if (current == mutation)
                level = player.LevelOf(mutation.Slot) % MutationData.MaxLevel + 1;
            if (player.IsEquipped(mutation))
                player.Unequip(mutation.Slot);
            player.Equip(mutation);
            while (player.LevelOf(mutation.Slot) < level && player.LevelUp(mutation.Slot))
            {
            }
            current = mutation;
            Say($"{mutation.DisplayName}: nivel {player.LevelOf(mutation.Slot)}");
        }

        void Clear()
        {
            foreach (var mutation in specials)
                if (mutation != null && player.IsEquipped(mutation))
                    player.Unequip(mutation.Slot);
            current = null;
            Say("Sin especial");
        }

        void HalfHeart()
        {
            if (health == null)
                return;
            health.Drain(health.Current - 1);
            Say("A medio corazón: que te golpeen para la Cápsula");
        }

        void FullHealth()
        {
            if (health == null)
                return;
            health.Heal(health.Max);
            Say("Vida completa");
        }

        void ToggleShooter()
        {
            shooting = !shooting;
            nextShot = Time.time + 0.5f;
            Say(shooting ? "Tirador de prueba: ENCENDIDO" : "Tirador de prueba: apagado");
        }

        void Shoot()
        {
            if (shooterAttack == null || shooterAttack.ProjectilePrefab == null)
                return;
            // Desde un lado al azar, hacia el jugador.
            Vector2 target = player.transform.position;
            Vector2 origin = target + Random.insideUnitCircle.normalized * shooterDistance;
            var projectile = ProjectilePool.Get(shooterAttack.ProjectilePrefab);
            projectile.Launch(origin, (target - origin).normalized, shooterAttack, CombatLayers.ProjectileLayer(Team.Enemy), null);
        }

        void SpawnDummy()
        {
            if (dummyEnemy == null)
                return;
            Vector2 aim = player.Controller != null ? player.Controller.AimDirection : Vector2.right;
            Instantiate(dummyEnemy, (Vector2)player.transform.position + aim.normalized * 5f, Quaternion.identity);
            Say("Enemigo delante: lanza la Gota (mantén el clic derecho y suelta)");
        }

        void Say(string text)
        {
            notice = text;
            noticeUntil = Time.time + 3f;
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            style.normal.textColor = Color.white;
            string state = current != null ? $"{current.DisplayName} (nivel {player.LevelOf(current.Slot)})" : "ninguna";
            string text = $"<b>Especial:</b> {state}    <b>1</b> Cercos  <b>2</b> Cápsula  <b>3</b> Gota (repetir = subir de nivel)  <b>0</b> quitar\n"
                + "<b>4</b> medio corazón  <b>5</b> vida llena  <b>6</b> tirador de prueba  <b>7</b> enemigo delante";
            if (Time.time < noticeUntil)
                text += $"\n<color=#ffd24a>{notice}</color>";
            GUI.Label(new Rect(12, 8, Screen.width - 24, 70), text, style);
        }
    }
}
