using Microsoft.Xna.Framework;
using System;
using Microsoft.Xna.Framework; // para MathHelper (se preferir)

namespace IsometricGame
{
    public static class IsoMath
    {
        public static Vector2 WorldToScreen(Vector3 worldPosition)
        {
            float screenX = (worldPosition.X - worldPosition.Y) * (Constants.IsoTileSize.X / 2f);
            float screenY = (worldPosition.X + worldPosition.Y) * (Constants.IsoTileSize.Y / 2f);

            // Subtrai a altura Z da posição Y na tela — cada unidade Z "levanta" o sprite em pixels
            screenY -= worldPosition.Z * Constants.TileHeightFactor;

            return new Vector2(screenX, screenY);
        }

        public static Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            float tileWidth = Constants.IsoTileSize.X;
            float tileHeight = Constants.IsoTileSize.Y;

            float worldX = (screenPosition.X / (tileWidth / 2f) + screenPosition.Y / (tileHeight / 2f)) / 2f;
            float worldY = (screenPosition.Y / (tileHeight / 2f) - (screenPosition.X / (tileWidth / 2f))) / 2f;
            return new Vector2(worldX, worldY);
        }

        // NOTE: esta função agora calcula depth baseado em X+Y (posição no chão).
        // O ajuste por Z deve ser feito fora (ex: Sprite.Draw subtrai WorldPosition.Z * zBias).
        public static float GetDepth(Vector3 worldPosition)
        {
            // Usa apenas X+Y como base
            float maxXY = Math.Max(1f, Constants.WorldSize.X + Constants.WorldSize.Y); // evita divisão por zero
            float currentXY = worldPosition.X + worldPosition.Y;

            float normalized = currentXY / maxXY; // 0..1
            // Inverte: 1.0 => topo/frente, 0.0 => base/fundo (compatível com BackToFront)
            return MathHelper.Clamp(1f - normalized, 0f, 1f);
        }
    }
}
