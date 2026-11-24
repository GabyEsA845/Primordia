using UnityEngine;

namespace Primordia.Combat
{
    /// <summary>
    /// El anillo de alcance de la Peonza (docs/multimedia-temp/floor3-spin-ring): se abre (<c>Appear</c>), da vueltas mientras sigue el
    /// giro (<c>Loop</c>) y se deshace (<c>End</c>) al soltar, marearse o esquivar. Solo es dibujo: el daño lo hace quien lo crea
    /// (<c>SpinAttack</c> o <c>SpinBoomerang</c>). Va dibujado a su tamaño real (el borde punteado es el área del daño): no se escala.
    /// </summary>
    public class SpinRing : MonoBehaviour
    {
        /// <summary>Cuadros del dibujo al abrirse que pasan antes de que el anillo empiece a dañar (el daño empieza en el cuadro 2).</summary>
        public const int DamageFrame = 2;

        SpriteRenderer render;
        Sprite[] appear, loop, end;
        float frameSeconds;
        float born;
        float endStart = -1f;

        /// <summary>Segundos desde que se abrió; sirve para saber si ya daña.</summary>
        public float Age => Time.time - born;

        public bool IsReleased => endStart >= 0f;

        /// <summary>Segundos del dibujo de apertura hasta el cuadro en que empieza el daño.</summary>
        public float DamageDelay => DamageFrame * frameSeconds;

        public static SpinRing Create(Transform parent, Sprite[] appear, Sprite[] loop, Sprite[] end, float frameSeconds, int sortingOrder)
        {
            var go = new GameObject("SpinRing");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            var ring = go.AddComponent<SpinRing>();
            ring.render = go.AddComponent<SpriteRenderer>();
            ring.render.sortingOrder = sortingOrder;
            ring.appear = appear;
            ring.loop = loop;
            ring.end = end;
            ring.frameSeconds = frameSeconds;
            ring.born = Time.time;
            ring.Show();
            return ring;
        }

        /// <summary>Pasa a deshacerse; al acabar su dibujo se quita sola (sin dibujo de fin, al momento).</summary>
        public void Release()
        {
            if (IsReleased)
                return;
            if (end == null || end.Length == 0)
            {
                Destroy(gameObject);
                return;
            }
            endStart = Time.time;
            Show();
        }

        void Update() => Show();

        void Show()
        {
            if (render == null)
                return;
            if (IsReleased)
            {
                int frame = Mathf.FloorToInt((Time.time - endStart) / frameSeconds);
                if (frame >= end.Length)
                {
                    Destroy(gameObject);
                    return;
                }
                render.sprite = end[frame];
                return;
            }

            float age = Time.time - born;
            int opening = Mathf.FloorToInt(age / frameSeconds);
            if (appear != null && opening < appear.Length)
                render.sprite = appear[opening];
            else if (loop != null && loop.Length > 0)
                render.sprite = loop[(opening - (appear?.Length ?? 0)) % loop.Length];
        }
    }
}
