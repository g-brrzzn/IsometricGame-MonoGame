using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.Collections.Generic;
using IsometricGame.Classes.Particles;
using System;

namespace IsometricGame.Classes
{
    public class Player : Sprite
    {
        private Dictionary<string, Texture2D> _sprites;
        private string _currentDirection = "south";

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; } = 0;
        public int ExperienceToNextLevel { get; private set; } = 5;
        public int MaxLife { get; private set; } = Constants.MaxLife;

        private float _attackCooldown = 0.8f;
        private float _attackTimer = 0f;
        private float _attackRange = 8.0f;
        private float _moveSpeedModifier = 1.0f;
        private float _knockbackStrength = 0f;
        public float MagnetRange { get; private set; } = 3.5f;        public int ProjectileCount { get; private set; } = 1;
        public int PiercingCount { get; private set; } = 0;

        private bool _movingRight, _movingLeft, _movingUp, _movingDown;
        private float _baseSpeed = 6.0f;

        public int Life { get; private set; }
        public Explosion ExplosionEffect { get; private set; }
        private double _lastHit;
        private double _invincibilityDuration = 1000;
        private const float _collisionRadius = .35f;
        private bool _isInvincible = false;

        public Player(Vector3 worldPos) : base(null, worldPos)
        {
            _sprites = new Dictionary<string, Texture2D>
            {
                { "south", GameEngine.Assets.Images["player_idle_south"] },
                { "west", GameEngine.Assets.Images["player_idle_west"] },
                { "north", GameEngine.Assets.Images["player_idle_north"] },
                { "east", GameEngine.Assets.Images["player_idle_east"] }
            };

            if (_sprites.ContainsKey(_currentDirection))
                UpdateTexture(_sprites[_currentDirection]);
            if (Texture != null)
                Origin = new Vector2(Texture.Width / 2f, Texture.Height);

            MaxLife = Constants.MaxLife;
            Life = MaxLife;
            ExplosionEffect = new Explosion();
        }

        public void BuffAttackSpeed(float percentage)
        {
            _attackCooldown *= (1.0f - percentage);
            _attackCooldown = Math.Max(0.1f, _attackCooldown);
        }
        public void BuffMoveSpeed(float percentage) => _moveSpeedModifier += percentage;
        public void BuffRange(float percentage) => _attackRange *= (1.0f + percentage);
        public void BuffMaxLife(int amount) { MaxLife += amount; Life += amount; }
        public void Heal(int amount) => Life = Math.Min(Life + amount, MaxLife);
        public void BuffKnockback(float amount) => _knockbackStrength += amount;

        public void BuffMagnet(float amount) => MagnetRange += amount;
        public void BuffProjectileCount(int amount) => ProjectileCount += amount;
        public void BuffPiercing(int amount) => PiercingCount += amount;

        public bool AddExperience(int amount)
        {
            Experience += amount;
            if (Experience >= ExperienceToNextLevel) return true;
            return false;
        }

        public void ConfirmLevelUp()
        {
            Level++;
            Experience -= ExperienceToNextLevel;
            ExperienceToNextLevel = (int)(ExperienceToNextLevel * 1.2f) + 5;
            if (Life < MaxLife) Life++;
        }

        public void GetInput(InputManager input)
        {
            _movingLeft = input.IsKeyDown("LEFT");
            _movingRight = input.IsKeyDown("RIGHT");
            _movingUp = input.IsKeyDown("UP");
            _movingDown = input.IsKeyDown("DOWN");
        }

        private void HandleAutoAttack(GameTime gameTime, float dt)
        {
            _attackTimer -= dt;

            if (_attackTimer <= 0)
            {
                EnemyBase closestEnemy = null;
                float closestDistSq = float.MaxValue;
                float rangeSq = _attackRange * _attackRange;

                foreach (var enemy in GameEngine.AllEnemies)
                {
                    if (enemy.IsRemoved) continue;
                    float distSq = Vector2.DistanceSquared(new Vector2(WorldPosition.X, WorldPosition.Y), new Vector2(enemy.WorldPosition.X, enemy.WorldPosition.Y));
                    if (distSq < rangeSq && distSq < closestDistSq) { closestDistSq = distSq; closestEnemy = enemy; }
                }

                if (closestEnemy != null)
                {
                    Vector2 direction = new Vector2(closestEnemy.WorldPosition.X - WorldPosition.X, closestEnemy.WorldPosition.Y - WorldPosition.Y);
                    if (direction != Vector2.Zero) direction.Normalize();

                    Fire(gameTime, direction);
                    _attackTimer = _attackCooldown;
                }
            }
        }

        private void Fire(GameTime gameTime, Vector2 worldAimDirection)
        {
            var options = new BulletOptions
            {
                SpeedScale = 12.0f,
                Piercing = this.PiercingCount,
                Knockback = this._knockbackStrength,
                Count = this.ProjectileCount,
                SpreadArc = 0.5f            };

            string pattern = (this.ProjectileCount > 1) ? "multishot" : "single";

            var bullets = Bullet.CreateBullets(
              pattern: pattern,
              worldPos: new Vector2(this.WorldPosition.X, this.WorldPosition.Y),
              worldDirection: worldAimDirection,
              isFromPlayer: true,
              options: options
            );

            foreach (var bullet in bullets)
            {
                GameEngine.PlayerBullets.Add(bullet);
                GameEngine.AllSprites.Add(bullet);
            }
            GameEngine.Assets.Sounds["shoot"].Play(0.4f, 0.2f, 0f);
        }

        private void Animate(Vector2 moveDirection)
        {
            string targetDirection = _currentDirection;
            if (moveDirection.LengthSquared() > 0)
            {
                if (moveDirection.Y > 0) targetDirection = "south";
                else if (moveDirection.Y < 0) targetDirection = "north";
                else if (moveDirection.X > 0) targetDirection = "east";
                else if (moveDirection.X < 0) targetDirection = "west";
            }
            _currentDirection = targetDirection;
            if (_sprites.ContainsKey(_currentDirection)) UpdateTexture(_sprites[_currentDirection]);
            if (Texture != null) Origin = new Vector2(Texture.Width / 2f, Texture.Height);
        }

        public override void Update(GameTime gameTime, float dt)
        {
            GetInput(Game1.InputManagerInstance);

            Vector2 worldDirection = Vector2.Zero;
            if (_movingUp) worldDirection += new Vector2(-1, -1);
            if (_movingDown) worldDirection += new Vector2(1, 1);
            if (_movingLeft) worldDirection += new Vector2(-1, 1);
            if (_movingRight) worldDirection += new Vector2(1, -1);

            if (worldDirection != Vector2.Zero) worldDirection.Normalize();

            Animate(worldDirection);
            HandleAutoAttack(gameTime, dt);

            ExplosionEffect.Update(dt);
            _isInvincible = gameTime.TotalGameTime.TotalMilliseconds - _lastHit < _invincibilityDuration;

            float currentSpeed = _baseSpeed * _moveSpeedModifier;
            Vector2 movement = worldDirection * currentSpeed * dt;

            Vector3 nextPos = WorldPosition + new Vector3(movement.X, 0, 0);
            if (IsCollidingAt(nextPos)) movement.X = 0;
            nextPos = WorldPosition + new Vector3(0, movement.Y, 0);
            if (IsCollidingAt(nextPos)) movement.Y = 0;

            WorldPosition += new Vector3(movement.X, movement.Y, 0);
            UpdateScreenPosition();
            WorldVelocity = (dt > 0) ? new Vector2(movement.X / dt, movement.Y / dt) : Vector2.Zero;
        }

        private bool IsCollidingAt(Vector3 futurePosition)
        {
            float baseZ = futurePosition.Z;
            Vector2 posXY = new Vector2(futurePosition.X, futurePosition.Y);
            Vector2 topLeft = posXY + new Vector2(-_collisionRadius, -_collisionRadius);
            Vector2 bottomRight = posXY + new Vector2(_collisionRadius, _collisionRadius);
            Vector3 cellTL = new Vector3(MathF.Round(topLeft.X), MathF.Round(topLeft.Y), baseZ);
            Vector3 cellBR = new Vector3(MathF.Round(bottomRight.X), MathF.Round(bottomRight.Y), baseZ);
            return GameEngine.SolidTiles.ContainsKey(cellTL) || GameEngine.SolidTiles.ContainsKey(cellBR);
        }

        public void TakeDamage(GameTime gameTime)
        {
            if (!_isInvincible)
            {
                Life -= 1;
                _lastHit = gameTime.TotalGameTime.TotalMilliseconds;
                _isInvincible = true;
                if (Texture != null)
                    ExplosionEffect.Create(this.ScreenPosition.X, this.ScreenPosition.Y - (this.Texture.Height / 2f), Constants.PlayerColorGreen, speed: -5);
                GameEngine.Assets.Sounds["hit"].Play();
                GameEngine.ScreenShake = 15;
                if (Life <= 0) Kill();
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Color tint = Color.White;
            if (_isInvincible && !IsRemoved)
            {
                if (((int)(DateTime.Now.TimeOfDay.TotalMilliseconds / 100f)) % 2 == 0) tint = Color.White * 0.5f;
            }

            Vector2 drawPosition = new Vector2(MathF.Round(ScreenPosition.X), MathF.Round(ScreenPosition.Y));
            float baseDepth = IsoMath.GetDepth(WorldPosition);
            float finalDepth = MathHelper.Clamp(baseDepth - 0.0001f, 0f, 1f);

            spriteBatch.Draw(Texture, drawPosition, null, tint, 0f, Origin, 1.0f, SpriteEffects.None, finalDepth);
            ExplosionEffect.Draw(spriteBatch);
        }
    }
}