using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace IsometricGame.Classes
{
    public class BulletOptions
    {
        public float? Angle { get; set; }
        public int? Count { get; set; }
        public float? SpreadArc { get; set; }
        public float? SpeedScale { get; set; } = 10.0f;    }

    public class Bullet : Sprite
    {
        public static Texture2D PlayerImage { get; set; }
        public static Texture2D EnemyImage { get; set; }

        public bool IsFromPlayer { get; private set; }

        public Bullet(Vector2 worldPosXY, Vector2 worldDirection, bool isFromPlayer, BulletOptions options = null)
            : base(null, new Vector3(worldPosXY.X, worldPosXY.Y, 0))
        {
            IsFromPlayer = isFromPlayer;
            options ??= new BulletOptions();            float speedScale = options.SpeedScale.Value;

            Texture = isFromPlayer ? PlayerImage : EnemyImage;

            if (Texture == null)
                Texture = Particles.Explosion.PixelTexture;


            if (worldDirection.LengthSquared() > 0)
            {
                WorldVelocity = Vector2.Normalize(worldDirection) * speedScale;
            }
            else
            {
                WorldVelocity = new Vector2(0, 1) * speedScale;            }
        }

        public static void LoadAssets(AssetManager assets)
        {
            PlayerImage = assets.Images["bullet_player"];
            EnemyImage = assets.Images["bullet_enemy"];
        }

        public static List<Bullet> CreateBullets(string pattern, Vector2 worldPos, Vector2 worldDirection, bool isFromPlayer, BulletOptions options = null)
        {
            var bullets = new List<Bullet>();
            options ??= new BulletOptions();

            if (pattern == "single")
            {
                bullets.Add(new Bullet(worldPos, worldDirection, isFromPlayer, options));
            }

            return bullets;
        }
        public override void Update(GameTime gameTime, float dt)
        {
            base.Update(gameTime, dt);

            float limit = Math.Max(Constants.WorldSize.X, Constants.WorldSize.Y) * 1.5f;            if (Math.Abs(WorldPosition.X) > limit || Math.Abs(WorldPosition.Y) > limit)
            {
                Kill();
            }
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture != null && !IsRemoved)
            {
                Vector2 floatingScreenPos = ScreenPosition - new Vector2(0, 12);
                Vector2 drawPosition = new Vector2(
                    MathF.Round(floatingScreenPos.X),
                    MathF.Round(floatingScreenPos.Y)
                );

                // --- INÍCIO DA CORREÇÃO DE FLICKERING ---
                float baseDepth = IsoMath.GetDepth(WorldPosition);
                const float zLayerBias = 0.001f;
                const float entityBias = 0.0001f; // Balas também são entidades

                float finalDepth = baseDepth - (WorldPosition.Z * zLayerBias) - entityBias;
                finalDepth = MathHelper.Clamp(finalDepth, 0f, 1f);
                // --- FIM DA CORREÇÃO ---

                spriteBatch.Draw(Texture,
                    drawPosition,
                    null,
                    Color.White,
                    0f,
                    Origin, // A origem (centro) padrão do Sprite.cs está OK para balas.
                    1.0f,
                    SpriteEffects.None,
                    finalDepth); // Usa a profundidade final corrigida
            }
        }
    }
}