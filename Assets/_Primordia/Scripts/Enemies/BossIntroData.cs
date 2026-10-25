using UnityEngine;

namespace Primordia.Enemies
{
    /// <summary>
    /// Lo que enseña el rótulo de la presentación de un jefe: el nombre ya rotulado (Jersey 10 con el color del piso),
    /// su título, su retrato y los colores de la banda. Uno por jefe.
    /// </summary>
    [CreateAssetMenu(menuName = "Primordia/Bosses/Boss Intro", fileName = "BossIntro_")]
    public class BossIntroData : ScriptableObject
    {
        [Tooltip("El nombre del jefe ya rotulado, en píxeles (boss-intros/Titles).")]
        [SerializeField] Sprite nameSprite;
        [Tooltip("Título que va debajo del nombre.")]
        [SerializeField] string title;
        [Tooltip("Retrato de 176×176 que entra por la derecha.")]
        [SerializeField] Sprite portrait;
        [Tooltip("Color de la banda diagonal (se dibuja casi opaca).")]
        [SerializeField] Color bandColor = new(0.12f, 0.05f, 0.1f, 1f);
        [Tooltip("Color del filo de la banda y del título.")]
        [SerializeField] Color accentColor = new(1f, 0.48f, 0.66f, 1f);

        public Sprite NameSprite => nameSprite;
        public string Title => title;
        public Sprite Portrait => portrait;
        public Color BandColor => bandColor;
        public Color AccentColor => accentColor;

        internal void Configure(Sprite newName, string newTitle, Sprite newPortrait, Color band, Color accent)
        {
            nameSprite = newName;
            title = newTitle;
            portrait = newPortrait;
            bandColor = band;
            accentColor = accent;
        }
    }
}
