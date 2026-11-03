using System;
using System.Collections.Generic;
using Primordia.Core;
using Primordia.Player;
using Primordia.Rooms;
using UnityEngine;

namespace Primordia.Mutations
{
    /// <summary>
    /// Comportamiento de <see cref="SpinnerLegsMutation"/>: al empezar la esquiva anota dónde estás y, al acabar, tiende un hilo
    /// hasta ahí. Solo caben unos pocos hilos a la vez: el más viejo se rompe para dejar sitio al nuevo. En el nivel III los hilos no
    /// se rompen (hasta salir de la sala) y al tender el último se unen en una red (<see cref="SilkNet"/>) y empieza de cero.
    /// </summary>
    public class SilkThreads : MonoBehaviour
    {
        // Segundos que se queda cada hilo del contorno de la red, visible, antes de soltarse.
        const float EdgeSeconds = 1.5f;

        PlayerController controller;
        int maxThreads;
        float seconds;
        int damage;
        float minLength;
        bool net;
        float netSeconds;
        float netHold;
        Sprite[] start, thread, anchor, hit, webAppear, webIdle, webFade;
        Vector2 origin;
        bool dodging;
        readonly List<SilkThread> laid = new();

        /// <summary>Se lanza con cada hilo tendido (los toques de los linajes se cuelgan de aquí).</summary>
        public event Action<SilkThread> Laid;

        /// <summary>Se lanza con cada red formada.</summary>
        public event Action<SilkNet> NetFormed;

        public int MaxThreads => maxThreads;

        /// <summary>Hilos tendidos ahora.</summary>
        public int LaidCount
        {
            get
            {
                laid.RemoveAll(t => t == null);
                return laid.Count;
            }
        }

        public void Configure(PlayerController newController, int newMax, float newSeconds, int newDamage, float newMinLength,
            bool newNet, float newNetSeconds, float newNetHold,
            Sprite[] newStart, Sprite[] newThread, Sprite[] newAnchor, Sprite[] newHit, Sprite[] newWebAppear, Sprite[] newWebIdle, Sprite[] newWebFade)
        {
            Unsubscribe();
            controller = newController;
            maxThreads = Mathf.Max(1, newMax);
            seconds = newSeconds;
            damage = newDamage;
            minLength = newMinLength;
            net = newNet;
            netSeconds = newNetSeconds;
            netHold = newNetHold;
            start = newStart;
            thread = newThread;
            anchor = newAnchor;
            hit = newHit;
            webAppear = newWebAppear;
            webIdle = newWebIdle;
            webFade = newWebFade;
            if (controller != null)
            {
                controller.DodgeStarted += OnDodgeStarted;
                controller.DodgeEnded += OnDodgeEnded;
            }
            Room.PlayerEnteredAny += OnRoomEntered;
        }

        void OnDestroy()
        {
            Unsubscribe();
            // Al perder la mutación o subirla de nivel los hilos tendidos se sueltan.
            foreach (var t in laid)
            {
                if (t != null)
                    Destroy(t.gameObject);
            }
        }

        void Unsubscribe()
        {
            Room.PlayerEnteredAny -= OnRoomEntered;
            if (controller == null)
                return;
            controller.DodgeStarted -= OnDodgeStarted;
            controller.DodgeEnded -= OnDodgeEnded;
        }

        // Los hilos permanentes del nivel III se quedan en su sala.
        void OnRoomEntered(Room room) => ClearThreads();

        void ClearThreads()
        {
            foreach (var t in laid)
            {
                if (t != null)
                    Destroy(t.gameObject);
            }
            laid.Clear();
        }

        void OnDodgeStarted()
        {
            origin = transform.position;
            dodging = true;
            SpriteFramePlayer.PlayOnce(start, 0.06f, origin, 6);
        }

        void OnDodgeEnded()
        {
            if (!dodging)
                return;
            dodging = false;
            Vector2 end = transform.position;
            if ((end - origin).magnitude < minLength)
                return;

            laid.RemoveAll(t => t == null);
            while (laid.Count >= maxThreads)
            {
                Destroy(laid[0].gameObject);
                laid.RemoveAt(0);
            }

            float life = seconds > 0f ? seconds : float.PositiveInfinity;
            var created = SilkThread.Spawn(origin, end, life, damage, thread, anchor, hit);
            laid.Add(created);
            Laid?.Invoke(created);
            if (net && laid.Count >= maxThreads)
                FormNet();
        }

        /// <summary>Une los extremos de los hilos tendidos, suelta los hilos y deja la red en su sitio.</summary>
        void FormNet()
        {
            var ends = new List<Vector2>();
            foreach (var t in laid)
            {
                ends.Add(t.From);
                ends.Add(t.To);
            }
            ClearThreads();

            var hull = SilkNet.ConvexHull(ends);
            if (hull.Count < 3)
                return;
            // Los hilos del contorno unen los extremos y se sueltan enseguida; el daño lo hace la red.
            for (int i = 0; i < hull.Count; i++)
                SilkThread.Spawn(hull[i], hull[(i + 1) % hull.Count], EdgeSeconds, 0, thread, anchor, hit);
            var created = SilkNet.Spawn(hull, netSeconds, damage, netHold, webAppear, webIdle, webFade);
            NetFormed?.Invoke(created);
        }
    }
}
