using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using IsometricGame.Classes.Particles;
using System.Collections.Generic;
using System;
using IsometricGame.Pathfinding;

namespace IsometricGame.Classes
{
    public class EnemyBase : Sprite
    {
        public static SoundEffect HitSound { get; set; }

        protected Dictionary<string, Texture2D> _sprites;
        protected string _currentDirection = "south";
        protected Explosion _explosion;

        public int Life { get; protected set; }
        public float Speed { get; protected set; } = 3.0f;
        public int Weight { get; protected set; }

        private double _lastHit;
        private double _hitFlashDuration = 100;
        private const float _collisionRadius = 0.35f;
        private bool _isHit = false;

        protected List<Vector3> _currentPath;
        protected float _pathTimer = 0f;
        protected const float _pathRefreshTime = 0.5f;        protected const float _nodeReachedThreshold = 0.5f;

        public EnemyBase(Vector3 worldPos, List<string> spriteKeys) : base(null, worldPos)
        {
            _sprites = LoadSprites(spriteKeys);
            _explosion = new Explosion();

            if (_sprites.Count > 0 && _sprites.ContainsKey(_currentDirection))
            {
                UpdateTexture(_sprites[_currentDirection]);
                if (Texture != null)
                    Origin = new Vector2(Texture.Width / 2f, Texture.Height);
            }

            if (Texture != null)
                _explosion.Create(this.ScreenPosition.X, this.ScreenPosition.Y - Texture.Height / 2f, speed: -5);
            Life = 3;
            Weight = 1;
        }

        public static void LoadAssets(AssetManager assets)
        {
            HitSound = assets.Sounds["hit"];
        }

        private Dictionary<string, Texture2D> LoadSprites(List<string> spriteKeys)
        {
            var dict = new Dictionary<string, Texture2D>();
            foreach (var key in spriteKeys)
            {
                if (GameEngine.Assets.Images.TryGetValue(key, out Texture2D texture))
                {
                    string direction = key.Contains("south") ? "south" : key.Contains("west") ? "west" : "south";
                    dict[direction] = texture;
                }
            }
            return dict;
        }

        public void Damage(GameTime gameTime)
        {
            _lastHit = gameTime.TotalGameTime.TotalMilliseconds;
            _isHit = true;
            if (Texture != null)
                _explosion.Create(this.ScreenPosition.X, this.ScreenPosition.Y - Texture.Height / 2f);

            Life -= 1;
            if (Life <= 0)
            {
                GameEngine.ScreenShake = Math.Max(GameEngine.ScreenShake, 5);

                DropExperience();

                Kill();
            }
        }

        private void DropExperience()
        {
            var gem = new ExperienceGem(this.WorldPosition, 1);            GameEngine.AllSprites.Add(gem);
        }


        public virtual void Move(GameTime gameTime, float dt)
        {
            if (GameEngine.Player == null || GameEngine.Player.IsRemoved)
            {
                WorldVelocity = Vector2.Zero;
                return;
            }

            _pathTimer -= dt;

            if (_pathTimer <= 0f)
            {
                _pathTimer = _pathRefreshTime;
                Vector2 direction = new Vector2(
                    GameEngine.Player.WorldPosition.X - WorldPosition.X,
                    GameEngine.Player.WorldPosition.Y - WorldPosition.Y
                );

                if (direction != Vector2.Zero) direction.Normalize();
                WorldVelocity = direction * Speed;

                if (Math.Abs(direction.X) > Math.Abs(direction.Y))
                    _currentDirection = direction.X > 0 ? "south" : "west";                else
                    _currentDirection = direction.Y > 0 ? "south" : "west";
            }

            if (_sprites.ContainsKey(_currentDirection))
            {
                UpdateTexture(_sprites[_currentDirection]);
                if (Texture != null) Origin = new Vector2(Texture.Width / 2f, Texture.Height);
            }
        }

        public override void Update(GameTime gameTime, float dt)
        {
            Move(gameTime, dt);

            Vector2 movement = WorldVelocity * dt;
            Vector3 nextPos = WorldPosition + new Vector3(movement.X, movement.Y, 0);

            if (!IsCollidingAt(nextPos))
            {
                WorldPosition += new Vector3(movement.X, movement.Y, 0);
            }

            UpdateScreenPosition();
            _explosion.Update(dt);

            if (_isHit && gameTime.TotalGameTime.TotalMilliseconds - _lastHit > _hitFlashDuration)
                _isHit = false;
        }

        private bool IsCollidingAt(Vector3 futurePosition)
        {
            Vector3 cell = new Vector3(MathF.Round(futurePosition.X), MathF.Round(futurePosition.Y), futurePosition.Z);
            return GameEngine.SolidTiles.ContainsKey(cell);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Color tint = _isHit ? Color.Red : Color.White;
            if (Texture != null && !IsRemoved)
            {
                Vector2 drawPosition = new Vector2(MathF.Round(ScreenPosition.X), MathF.Round(ScreenPosition.Y));
                float baseDepth = IsoMath.GetDepth(WorldPosition);
                spriteBatch.Draw(Texture, drawPosition, null, tint, 0f, Origin, 1.0f, SpriteEffects.None, baseDepth);
            }
            _explosion.Draw(spriteBatch);
        }
    }
}