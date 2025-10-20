using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System;

namespace IsometricGame.Pathfinding
{
    public static class Pathfinder
    {
        // Custo para se mover para um tile adjacente (10 = 1.0)
        private const int MOVE_STRAIGHT_COST = 10;
        // Custo para se mover para um tile diagonal (14 ~ 1.4 * 10)
        private const int MOVE_DIAGONAL_COST = 14;

        /// <summary>
        /// Encontra um caminho entre duas posições do mundo usando A*.
        /// </summary>
        /// <returns>Uma lista de Vector3 (posições do mundo) ou null se nenhum caminho for encontrado.</returns>
        public static List<Vector3> FindPath(Vector3 startWorldPos, Vector3 targetWorldPos)
        {
            // 1. Arredonda as posições para garantir que estamos no grid
            Vector3 startPos = new Vector3(MathF.Round(startWorldPos.X), MathF.Round(startWorldPos.Y), startWorldPos.Z);
            Vector3 targetPos = new Vector3(MathF.Round(targetWorldPos.X), MathF.Round(targetWorldPos.Y), targetWorldPos.Z);

            PathNode startNode = new PathNode(startPos);
            PathNode targetNode = new PathNode(targetPos);
            startNode.CalculateHCost(targetPos);

            // Lista de nós a serem avaliados
            List<PathNode> openList = new List<PathNode> { startNode };
            // HashSet para posições de nós já avaliados (busca rápida)
            HashSet<Vector3> closedList = new HashSet<Vector3>();

            while (openList.Count > 0)
            {
                // 2. Encontra o nó com o menor F_Cost na openList
                PathNode currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].F_Cost < currentNode.F_Cost ||
                       (openList[i].F_Cost == currentNode.F_Cost && openList[i].H_Cost < currentNode.H_Cost))
                    {
                        currentNode = openList[i];
                    }
                }

                // 3. Move o nó atual da openList para a closedList
                openList.Remove(currentNode);
                closedList.Add(currentNode.Position);

                // 4. Chegou ao destino?
                if (currentNode.Position == targetNode.Position)
                {
                    return ReconstructPath(currentNode);
                }

                // 5. Itera pelos vizinhos
                foreach (PathNode neighbor in GetNeighbors(currentNode, targetPos))
                {
                    // 6. Verifica se o vizinho é caminhável e não está na closedList
                    if (closedList.Contains(neighbor.Position))
                        continue;

                    // REGRA DE "CAMINHÁVEL":
                    // Um tile não é caminhável se ele existir no dicionário de Sólidos.
                    // (Isso inclui água no Z=0 e qualquer tile com Z > 0)
                    if (GameEngine.SolidTiles.ContainsKey(neighbor.Position))
                    {
                        closedList.Add(neighbor.Position); // Marca como "não caminhável"
                        continue;
                    }

                    // 7. Calcula o novo custo G para o vizinho
                    if (!openList.Any(n => n.Position == neighbor.Position))
                    {
                        // Se não está na openList, calcula custos e adiciona
                        neighbor.G_Cost = currentNode.G_Cost + CalculateMovementCost(currentNode, neighbor);
                        neighbor.CalculateHCost(targetPos);
                        neighbor.Parent = currentNode;
                        openList.Add(neighbor);
                    }
                    else
                    {
                        // Se já está na openList, verifica se este caminho é *melhor*
                        int newGCost = currentNode.G_Cost + CalculateMovementCost(currentNode, neighbor);
                        if (newGCost < neighbor.G_Cost)
                        {
                            neighbor.G_Cost = newGCost;
                            neighbor.Parent = currentNode;
                        }
                    }
                }
            }

            // Nenhum caminho encontrado
            return null;
        }

        /// <summary>
        /// Retorna os 8 vizinhos de um nó.
        /// </summary>
        private static List<PathNode> GetNeighbors(PathNode currentNode, Vector3 targetPos)
        {
            List<PathNode> neighbors = new List<PathNode>();
            Vector3 pos = currentNode.Position;

            // 8 direções (Ortogonais e Diagonais)
            // (Ignoramos o Z, pois o pathfinding é 2D no nível Z atual)
            int z = (int)pos.Z;
            neighbors.Add(new PathNode(new Vector3(pos.X + 1, pos.Y, z)));
            neighbors.Add(new PathNode(new Vector3(pos.X - 1, pos.Y, z)));
            neighbors.Add(new PathNode(new Vector3(pos.X, pos.Y + 1, z)));
            neighbors.Add(new PathNode(new Vector3(pos.X, pos.Y - 1, z)));
            neighbors.Add(new PathNode(new Vector3(pos.X + 1, pos.Y + 1, z)));
            neighbors.Add(new PathNode(new Vector3(pos.X - 1, pos.Y - 1, z)));
            neighbors.Add(new PathNode(new Vector3(pos.X - 1, pos.Y + 1, z)));
            neighbors.Add(new PathNode(new Vector3(pos.X + 1, pos.Y - 1, z)));

            return neighbors;
        }

        /// <summary>
        /// Calcula o custo de movimento (10 para reto, 14 para diagonal).
        /// </summary>
        private static int CalculateMovementCost(PathNode from, PathNode to)
        {
            bool isDiagonal = (from.Position.X != to.Position.X) && (from.Position.Y != to.Position.Y);
            return isDiagonal ? MOVE_DIAGONAL_COST : MOVE_STRAIGHT_COST;
        }

        /// <summary>
        /// Reconstrói o caminho de volta do nó final para o inicial.
        /// </summary>
        private static List<Vector3> ReconstructPath(PathNode endNode)
        {
            List<Vector3> path = new List<Vector3>();
            PathNode currentNode = endNode;

            while (currentNode.Parent != null)
            {
                path.Add(currentNode.Position);
                currentNode = currentNode.Parent;
            }
            path.Reverse(); // Inverte para ter do Início -> Fim
            return path;
        }
    }
}