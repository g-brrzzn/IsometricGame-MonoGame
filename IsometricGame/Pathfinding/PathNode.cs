using Microsoft.Xna.Framework;
using System;

namespace IsometricGame.Pathfinding
{
    /// <summary>
    /// Representa um nó no grid para o algoritmo A*.
    /// </summary>
    public class PathNode
    {
        public Vector3 Position { get; }
        public PathNode Parent { get; set; }

        /// <summary>
        /// G-Cost: Custo do início até este nó.
        /// </summary>
        public int G_Cost { get; set; }

        /// <summary>
        /// H-Cost: Custo Heurístico (estimado) deste nó até o fim.
        /// </summary>
        public int H_Cost { get; set; }

        /// <summary>
        /// F-Cost: G_Cost + H_Cost
        /// </summary>
        public int F_Cost => G_Cost + H_Cost;

        public PathNode(Vector3 position)
        {
            Position = position;
        }

        /// <summary>
        /// Calcula a Heurística (Distância de Manhattan) para o alvo.
        /// Ignora Z para o cálculo de distância.
        /// </summary>
        public void CalculateHCost(Vector3 targetPosition)
        {
            // Usamos custos 10 para ortogonal e 14 para diagonal (para evitar floats)
            int dX = (int)Math.Abs(Position.X - targetPosition.X);
            int dY = (int)Math.Abs(Position.Y - targetPosition.Y);

            // Heurística Octogonal (para 8 direções)
            int min_d = Math.Min(dX, dY);
            int max_d = Math.Max(dX, dY);
            H_Cost = (min_d * 14) + ((max_d - min_d) * 10);

            // Heurística de Manhattan (para 4 direções)
            // H_Cost = (dX + dY) * 10;
        }
    }
}