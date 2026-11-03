using System;
using Primordia.Combat;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="AmphibianSkinMutation"/>. El moco se carga mientras el jugador pisa agua, lodo o un charco (sin
    /// esquivar). Cargado, es inmune al fuego y al veneno de las plantas (es un <see cref="FireImmune"/> y un
    /// <see cref="IPoisonImmune"/> solo mientras está cargado) y apaga el quemado. Si recibe daño, el moco se rompe; con el Bloqueo
    /// (nivel III) ese daño no llega a hacerse, solo se rompe el moco (más 0,4 s de invulnerabilidad para que un contacto no cuente
    /// dos veces).
    /// </summary>
    public class AmphibianMucus : FireImmune, IPoisonImmune
    {
        const int FxOrder = 22;
        const float AfterBreak = 0.4f;
        // Los pies respecto al centro del cuerpo.
        static readonly Vector2 FootOffset = new(0f, -0.3f);

        PlayerController controller;
        Health health;
        float chargeSeconds = 3f;
        bool blocks;
        Sprite[] chargeFx, shieldFx, breakFx, blockFx;
        float charge;
        GameObject chargeObject;
        GameObject shieldObject;

        /// <summary>Carga del moco, de 0 a 1.</summary>
        public float Charge01 => Charged ? 1f : Mathf.Clamp01(charge / chargeSeconds);

        /// <summary>True con el moco cargado: protege del fuego y del veneno.</summary>
        public bool Charged { get; private set; }

        /// <summary>Veces que se ha roto el moco (para las pruebas).</summary>
        public int Breaks { get; private set; }

        public override bool IsImmune => Charged;

        public bool ImmuneToPlantPoison => Charged;

        /// <summary>Se lanza cuando el moco se rompe; el parámetro dice si bloqueó el golpe (no hubo daño).</summary>
        public event Action<bool> Broken;

        public void Configure(PlayerController newController, Health newHealth, float newChargeSeconds, bool newBlocks,
            Sprite[] newCharge, Sprite[] newShield, Sprite[] newBreak, Sprite[] newBlock)
        {
            Unsubscribe();
            controller = newController;
            health = newHealth;
            chargeSeconds = Mathf.Max(0.1f, newChargeSeconds);
            blocks = newBlocks;
            chargeFx = newCharge;
            shieldFx = newShield;
            breakFx = newBreak;
            blockFx = newBlock;
            if (health != null)
                health.Damaged += OnDamaged;
            if (blocks)
                Health.AddDamageModifier(BlockDamage);
        }

        void OnDestroy()
        {
            Unsubscribe();
            HideChargeFx();
            HideShieldFx();
        }

        void Unsubscribe()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
            Health.RemoveDamageModifier(BlockDamage);
        }

        void Update()
        {
            if (Charged || controller == null || (health != null && health.IsDead))
            {
                HideChargeFx();
                return;
            }

            bool wet = !controller.IsDodging && Wetness.IsWet((Vector2)transform.position + FootOffset);
            if (!wet)
            {
                HideChargeFx();
                return;
            }

            charge += Time.deltaTime;
            ShowChargeFx();
            if (charge >= chargeSeconds)
            {
                Charged = true;
                HideChargeFx();
                ShowShieldFx();
                // El moco apaga lo que ardiera.
                if (TryGetComponent<PlayerBurn>(out var burn))
                    burn.Douse(1f);
            }
        }

        /// <summary>Bloqueo (nivel III): el daño que iba a recibir el jugador con el moco puesto no llega; el moco se rompe.</summary>
        int BlockDamage(Health target, int amount)
        {
            if (!Charged || target != health || amount <= 0)
                return amount;
            Break(blocked: true);
            return 0;
        }

        // Sin Bloqueo, el golpe llega y además rompe el moco.
        void OnDamaged(int _)
        {
            if (Charged)
                Break(blocked: false);
        }

        void Break(bool blocked)
        {
            Charged = false;
            charge = 0f;
            Breaks++;
            HideShieldFx();
            if (blocked && health != null)
                health.MakeInvulnerable(AfterBreak);
            FrameEffect.Play("MucusBreak", blocked && blockFx != null && blockFx.Length > 0 ? blockFx : breakFx, 0.06f, FxOrder, transform);
            Broken?.Invoke(blocked);
        }

        // ---------- Efectos ----------

        void ShowChargeFx()
        {
            if (chargeObject == null)
                chargeObject = LoopFx("MucusCharge", chargeFx);
        }

        void HideChargeFx()
        {
            if (chargeObject != null)
                Destroy(chargeObject);
            chargeObject = null;
        }

        void ShowShieldFx()
        {
            if (shieldObject == null)
                shieldObject = LoopFx("MucusShield", blocks && blockFx != null && blockFx.Length > 0 ? blockFx : shieldFx);
        }

        void HideShieldFx()
        {
            if (shieldObject != null)
                Destroy(shieldObject);
            shieldObject = null;
        }

        GameObject LoopFx(string fxName, Sprite[] frames)
        {
            if (frames == null || frames.Length == 0)
                return null;
            var obj = new GameObject(fxName);
            obj.transform.SetParent(transform, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = FxOrder;
            obj.AddComponent<SpriteFramePlayer>().Play(renderer, frames, 0.1f, newLoop: true);
            return obj;
        }
    }
}
