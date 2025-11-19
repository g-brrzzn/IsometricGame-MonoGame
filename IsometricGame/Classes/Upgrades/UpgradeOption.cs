using Microsoft.Xna.Framework;
using System;

namespace IsometricGame.Classes.Upgrades
{
    public class UpgradeOption
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public Color Color { get; set; }
        public Action<Player> ApplyEffect { get; set; }

        public UpgradeOption(string title, string description, Color color, Action<Player> applyEffect)
        {
            Title = title;
            Description = description;
            Color = color;
            ApplyEffect = applyEffect;
        }
    }
}