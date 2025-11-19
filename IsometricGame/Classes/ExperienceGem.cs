using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace IsometricGame.Classes
{
    public class ExperienceGem : Sprite
    {
        public int Value { get; private set; }
        private float _magnetSpeed = 0f;
        private const float _acceleration = 15f;
        private const float _maxSpeed = 400f;
        private bool _isMagnetized = false;
        private float _floatTimer = 0f;

        public ExperienceGem(Vector3 worldPos, int value) : base(null, worldPos)
        {
            Value = value;

            if (GameEngine.Assets.Images.ContainsKey("gem"))
                UpdateTexture(GameEngine.Assets.Images["gem"]);
            else
                UpdateTexture(GameEngine.Assets.Images["bullet_player"]);
            BaseYOffsetWorld = 10f;
        }

        public override void Update(GameTime gameTime, float dt)
        {
            base.Update(gameTime, dt);

            if (GameEngine.Player == null || GameEngine.Player.IsRemoved) return;

            _floatTimer += dt * 5f;
            if (!_isMagnetized)
            {
                BaseYOffsetWorld = (float)Math.Sin(_floatTimer) * 3f + 5f;
            }

            float distToPlayerSq = Vector2.DistanceSquared(
                new Vector2(WorldPosition.X, WorldPosition.Y),
                new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y));

            float magnetRadiusSq = 3.5f * 3.5f;

            if (distToPlayerSq < magnetRadiusSq || _isMagnetized)
            {
                _isMagnetized = true;
                _magnetSpeed += _acceleration * dt * 60f;                _magnetSpeed = Math.Min(_magnetSpeed, _maxSpeed);

                Vector2 direction = new Vector2(
                    GameEngine.Player.WorldPosition.X - WorldPosition.X,
                    GameEngine.Player.WorldPosition.Y - WorldPosition.Y
                );

                if (direction != Vector2.Zero) direction.Normalize();

                WorldPosition += new Vector3(direction.X, direction.Y, 0) * _magnetSpeed * dt * 0.05f;
                BaseYOffsetWorld = MathHelper.Lerp(BaseYOffsetWorld, 10f, dt * 5);
                if (distToPlayerSq < 0.5f * 0.5f)
                {
                    GameEngine.Player.AddExperience(Value);
                    GameEngine.Assets.Sounds["menu_select"].Play(0.3f, 0.5f, 0f);
                    Kill();
                }
            }
        }
    }
}