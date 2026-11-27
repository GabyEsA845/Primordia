using System;
using Primordia.Enemies;
using UnityEngine;

namespace Primordia.UI
{
    public enum BestiaryTab
    {
        Floor1,
        Floor2,
        Floor3,
        Bosses
    }

    /// <summary>Una criatura del bestiario.</summary>
    [Serializable]
    public class BestiaryEntry
    {
        public string id;
        public string displayName;
        public BestiaryTab tab;
        [Tooltip("Trampa o planta: va en su fila aparte y no tiene vida.")]
        public bool trap;
        [Tooltip("No tiene casilla hasta que se encuentra.")]
        public bool secret;
        [Tooltip("Nota de naturalista, una línea por elemento.")]
        public string[] notes = Array.Empty<string>();
        [Tooltip("De aquí sale la vida. Vacío en las trampas.")]
        public EnemyData data;
        public string damage;
        public string speed;
        public Sprite portrait;
        public Sprite lockedPortrait;
        [Tooltip("Animación de la ficha. Vacía: se usa el retrato a 3x.")]
        public Sprite[] frames = Array.Empty<Sprite>();
        [Tooltip("Tamaño del dibujo (sin el margen transparente) y su centro respecto al del cuadro, en píxeles.")]
        public Vector2 contentSize;
        public Vector2 contentOffset;
    }

    /// <summary>
    /// Sprites, fuentes y datos del menú de pausa y del bestiario (Resources/PauseMenuArt). Lo crea
    /// Primordia > Art > Importar menú de pausa.
    /// </summary>
    public class PauseMenuArt : ScriptableObject
    {
        [Header("Fuentes")]
        public Font font;
        public Font boldFont;

        [Header("Paneles y botones")]
        public Sprite panel;
        public Sprite title;
        public Sprite buttonNormal, buttonHighlighted, buttonPressed;
        public Sprite dangerNormal, dangerHighlighted, dangerPressed;
        public Sprite[] nucleus, nucleusDanger;

        [Header("Pestañas")]
        public Sprite tabActive, tabIdle, tabHover;
        [Tooltip("Criatura, Estado, Objetos, Partida.")]
        public Sprite[] characterIcons;
        [Tooltip("Piso 1, Piso 2, Piso 3, Jefes.")]
        public Sprite[] bestiaryIcons;
        [Tooltip("Normal y pulsada.")]
        public Sprite[] keyQ, keyE;

        [Header("Casillas, filas y cuadro de información")]
        public Sprite slot, slotHighlighted;
        public Sprite row;
        public Sprite tooltip, tooltipPod, tooltipStripe;

        [Header("Estado")]
        public Sprite heartFull, heartHalf, heartEmpty;
        public Sprite[] heartExtraFull;
        public Sprite heartExtraHalf, heartExtraEmpty;
        public Sprite damageIcon, speedIcon, unstableIcon, upArrow, downArrow;

        [Header("Objetos y Partida")]
        public Sprite biomassIcon;
        public Sprite floorIcon, seedIcon, timeIcon, enemiesIcon, bossesIcon;

        [Header("Configuración")]
        public Sprite musicIcon, effectsIcon, shakeIcon;
        public Sprite sliderTrack, sliderFill, sliderKnob;
        public Sprite toggleOff, toggleOn;

        [Header("Bestiario")]
        public Sprite beastSlot, beastSlotHighlighted, beastSlotLocked;
        public Sprite vacuole, vacuoleGlint;
        public BestiaryEntry[] entries = Array.Empty<BestiaryEntry>();

        static PauseMenuArt current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => current = null;

        public static PauseMenuArt Current => current != null ? current : current = Resources.Load<PauseMenuArt>("PauseMenuArt");
    }
}
