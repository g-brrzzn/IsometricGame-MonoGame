using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace IsometricGame.Classes
{
    public class Enemy1 : EnemyBase
    {
        // Construtor de compatibilidade (aceita Vector2 e passa Z=0)
        public Enemy1(Vector2 worldPosXY)
            : this(new Vector3(worldPosXY.X, worldPosXY.Y, 0)) { }

        // --- CONSTRUTOR PRINCIPAL (aceita Vector3) ---
        public Enemy1(Vector3 worldPos)
            : base(worldPos, new List<string> {
                "enemy1_idle_south",
                "enemy1_idle_west",
                "enemy1_idle_north", // Adicionado
                "enemy1_idle_east"   // Adicionado
            })
        {
            Life = 1;
            Weight = 1;
            Speed = 3.0f;
        }
    }
}