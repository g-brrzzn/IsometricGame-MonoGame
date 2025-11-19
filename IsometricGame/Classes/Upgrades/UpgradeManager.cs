using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace IsometricGame.Classes.Upgrades
{
    public static class UpgradeManager
    {
        private static List<UpgradeOption> _allUpgrades;

        public static void Initialize()
        {
            _allUpgrades = new List<UpgradeOption>
            {
                new UpgradeOption(
                    "Coffee",
                    "Increases Attack Speed by 15%.",
                    Color.Orange,
                    p => p.BuffAttackSpeed(0.15f)
                ),
                new UpgradeOption(
                    "Running Shoes",
                    "Increases Movement Speed by 10%.",
                    Color.CornflowerBlue,
                    p => p.BuffMoveSpeed(0.10f)
                ),
                new UpgradeOption(
                    "Laser Scope",
                    "Increases Attack Range by 20%.",
                    Color.Red,
                    p => p.BuffRange(0.20f)
                ),
                new UpgradeOption(
                    "First Aid Kit",
                    "Heals 1 HP immediately.",
                    Color.Green,
                    p => p.Heal(1)
                ),
                new UpgradeOption(
                    "Heart Container",
                    "Increases Max HP by 1 and heals.",
                    Color.Purple,
                    p => p.BuffMaxLife(1)
                ),
                new UpgradeOption(
                    "Heavy Rounds",
                    "Increases Knockback power.",
                    Color.Gray,
                    p => p.BuffKnockback(1.5f)
                ),
                new UpgradeOption(
                    "Magnet",
                    "Increases XP Pickup Range by 50%.",
                    Color.Cyan,
                    p => p.BuffMagnet(1.5f)
                ),
                new UpgradeOption(
                    "Twin Shot",
                    "Adds +1 Projectile to your weapon.",
                    Color.Gold,
                    p => p.BuffProjectileCount(1)
                ),
                new UpgradeOption(
                    "FMJ Rounds",
                    "Bullets now pierce through +1 enemy.",
                    Color.DarkRed,
                    p => p.BuffPiercing(1)
                )
            };
        }

        public static List<UpgradeOption> GetRandomOptions(int count)
        {
            if (_allUpgrades == null) Initialize();

            return _allUpgrades.OrderBy(x => GameEngine.Random.Next()).Take(count).ToList();
        }
    }
}