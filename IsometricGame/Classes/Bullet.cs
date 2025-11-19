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
        public float? SpeedScale { get; set; } = 10.0f;
        public int Piercing { get; set; } = 0;        public float Knockback { get; set; } = 0f;
    }

    public class Bullet : Sprite
    {
        public static Texture2D PlayerImage { get; set; }
        public static Texture2D EnemyImage { get; set; }

        public bool IsFromPlayer { get; private set; }
        public int PiercingLeft { get; set; }
        public float KnockbackPower { get; set; }

        public HashSet<EnemyBase> HitList { get; private set; } = new HashSet<EnemyBase>();

        public Bullet(Vector2 worldPosXY, Vector2 worldDirection, bool isFromPlayer, BulletOptions options = null)
            : base(null, new Vector3(worldPosXY.X, worldPosXY.Y, 0))
        {
            IsFromPlayer = isFromPlayer;
            options ??= new BulletOptions();
            float speedScale = options.SpeedScale.Value;
            PiercingLeft = options.Piercing;
            KnockbackPower = options.Knockback;

            Texture = isFromPlayer ? PlayerImage : EnemyImage;
            if (Texture == null) Texture = Particles.Explosion.PixelTexture;

            if (worldDirection.LengthSquared() > 0)
                WorldVelocity = Vector2.Normalize(worldDirection) * speedScale;
            else
                WorldVelocity = new Vector2(0, 1) * speedScale;

            BaseYOffsetWorld = 8f;
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
            else if (pattern == "multishot")
            {
                int count = options.Count ?? 1;
                float arc = options.SpreadArc ?? 0.5f;
                float baseAngle = MathF.Atan2(worldDirection.Y, worldDirection.X);
                float startAngle = baseAngle - arc / 2f;
                float step = (count > 1) ? arc / (count - 1) : 0;

                for (int i = 0; i < count; i++)
                {
                    float currentAngle = startAngle + (step * i);
                    Vector2 dir = new Vector2(MathF.Cos(currentAngle), MathF.Sin(currentAngle));
                    bullets.Add(new Bullet(worldPos, dir, isFromPlayer, options));
                }
            }

            return bullets;
        }

        public override void Update(GameTime gameTime, float dt)
        {
            base.Update(gameTime, dt);

            float limit = Math.Max(Constants.WorldSize.X, Constants.WorldSize.Y) * 1.5f;
            if (Math.Abs(WorldPosition.X) > limit || Math.Abs(WorldPosition.Y) > limit)
            {
                Kill();
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Texture != null && !IsRemoved)
            {
                Vector2 floatingScreenPos = IsoMath.WorldToScreen(new Vector3(WorldPosition.X, WorldPosition.Y, WorldPosition.Z));
                floatingScreenPos.Y -= BaseYOffsetWorld;

                Vector2 drawPosition = new Vector2(MathF.Round(floatingScreenPos.X), MathF.Round(floatingScreenPos.Y));

                float baseDepth = IsoMath.GetDepth(WorldPosition);
                float finalDepth = MathHelper.Clamp(baseDepth - 0.0002f, 0f, 1f);

                spriteBatch.Draw(Texture, drawPosition, null, Color.White, 0f, Origin, 1.0f, SpriteEffects.None, finalDepth);
            }
        }
    }
}