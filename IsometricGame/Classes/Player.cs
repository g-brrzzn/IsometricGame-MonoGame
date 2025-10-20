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

        private double _shotDelay = 0.25;
        private double _lastShot;

        private bool _movingRight, _movingLeft, _movingUp, _movingDown, _firing;
        private float _speed = 6.0f;
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
                // Esta linha está CORRETA. Ela sobrescreve a origem do Sprite base.
                Origin = new Vector2(Texture.Width / 2f, Texture.Height);

            Life = Constants.MaxLife;
            ExplosionEffect = new Explosion();
        }

        public void GetInput(InputManager input)
        {
            _movingLeft = input.IsKeyDown("LEFT");
            _movingRight = input.IsKeyDown("RIGHT");
            _movingUp = input.IsKeyDown("UP");
            _movingDown = input.IsKeyDown("DOWN");

            // --- MODIFICAÇÃO 1: Atirar com Espaço OU Mouse ---
            _firing = input.IsKeyDown("FIRE") || input.IsLeftMouseButtonDown();
            // --- FIM DA MODIFICAÇÃO 1 ---
        }

        // --- ADIÇÃO 2: Novo método para calcular a mira ---
        /// <summary>
        /// Calcula o vetor de direção normalizado do player para o mouse.
        /// </summary>
        private Vector2 GetAimDirection(InputManager input)
        {
            // 1. Pega a posição do mouse na resolução interna (1600x900)
            Vector2 mouseInternalPos = input.InternalMousePosition;

            // 2. Converte a posição da tela interna para o mundo do jogo (levando em conta a câmera)
            Vector2 mouseWorldPos = Game1.Camera.ScreenToWorld(mouseInternalPos);

            // 3. Calcula o vetor de direção do player (mundo) para o mouse (mundo)
            Vector2 aimDirection = mouseWorldPos - new Vector2(this.WorldPosition.X, this.WorldPosition.Y);

            // 4. Normaliza o vetor (transforma em um vetor de comprimento 1)
            if (aimDirection.LengthSquared() > 0)
            {
                aimDirection.Normalize();
            }
            return aimDirection;
        }
        // --- FIM DA ADIÇÃO 2 ---

        // --- MODIFICAÇÃO 3: Método Fire agora usa a direção da mira ---
        private void Fire(GameTime gameTime, Vector2 worldAimDirection)
        {
            _lastShot = gameTime.TotalGameTime.TotalSeconds;

            // O vetor da mira (worldAimDirection) JÁ é a direção que queremos atirar.
            // Não precisamos mais do switch/case.
            Vector2 shotDirection = worldAimDirection;

            // Failsafe: Se a mira estiver exatamente no player (vetor 0), atira para "south"
            if (shotDirection.LengthSquared() == 0)
            {
                shotDirection = new Vector2(1, 1);
            }

            var bullets = Bullet.CreateBullets(
              pattern: "single",
              worldPos: new Vector2(this.WorldPosition.X, this.WorldPosition.Y),
              worldDirection: shotDirection, // Usa a direção da mira
                      isFromPlayer: true
            );

            foreach (var bullet in bullets)
            {
                GameEngine.PlayerBullets.Add(bullet);
                GameEngine.AllSprites.Add(bullet);
            }
            GameEngine.Assets.Sounds["shoot"].Play();
        }
        // --- FIM DA MODIFICAÇÃO 3 ---

        // --- MODIFICAÇÃO 4: Animate agora usa a mira para definir o sprite ---
        private void Animate(Vector2 worldAimDirection)
        {
            string targetDirection = _currentDirection;

            // 1. Define a direção com base no movimento
            if (_movingUp) targetDirection = "north";
            else if (_movingDown) targetDirection = "south";
            else if (_movingLeft) targetDirection = "west";
            else if (_movingRight) targetDirection = "east";

            // 2. Sobrescreve a direção com base na mira (se estivermos mirando)
            // A mira tem prioridade sobre o movimento para o sprite.
            if (worldAimDirection.LengthSquared() > 0)
            {
                // Precisamos converter a direção da mira do MUNDO para a TELA
                // para sabermos qual sprite (N, S, L, O) usar.
                Vector2 screenAim = new Vector2(
                    worldAimDirection.X - worldAimDirection.Y, // (WorldX - WorldY) = ScreenX
                    worldAimDirection.X + worldAimDirection.Y  // (WorldX + WorldY) = ScreenY
                );

                if (Math.Abs(screenAim.X) > Math.Abs(screenAim.Y))
                {
                    // Mira está mais na horizontal (tela)
                    targetDirection = (screenAim.X > 0) ? "east" : "west";
                }
                else
                {
                    // Mira está mais na vertical (tela)
                    targetDirection = (screenAim.Y > 0) ? "south" : "north";
                }
            }

            _currentDirection = targetDirection;

            if (_sprites.ContainsKey(_currentDirection))
                UpdateTexture(_sprites[_currentDirection]);
            if (Texture != null)
                Origin = new Vector2(Texture.Width / 2f, Texture.Height);
        }
        // --- FIM DA MODIFICAÇÃO 4 ---

        // --- MODIFICAÇÃO 5: Update agora orquestra a mira, animação e tiro ---
        public override void Update(GameTime gameTime, float dt)
        {
            double totalMilliseconds = gameTime.TotalGameTime.TotalMilliseconds;

            // Pega o input e a mira usando o InputManager estático do Game1
            GetInput(Game1.InputManagerInstance);
            Vector2 worldAimDirection = GetAimDirection(Game1.InputManagerInstance);

            ExplosionEffect.Update(dt);
            Animate(worldAimDirection); // Anima baseado na mira
            _isInvincible = totalMilliseconds - _lastHit < _invincibilityDuration;

            // --- Lógica de Direção (Sem Mudanças) ---
            Vector2 worldDirection = Vector2.Zero;
            if (_movingUp) worldDirection += new Vector2(-1, -1);
            if (_movingDown) worldDirection += new Vector2(1, 1);
            if (_movingLeft) worldDirection += new Vector2(-1, 1);
            if (_movingRight) worldDirection += new Vector2(1, -1);

            if (worldDirection != Vector2.Zero)
            {
                worldDirection.Normalize();
            }

            Vector2 desiredVelocity = worldDirection * _speed;

            // --- INÍCIO DA LÓGICA DE COLISÃO AABB vs GRID ---

            Vector2 movement = desiredVelocity * dt;

            // 1. CHECA EIXO X
            Vector3 nextPos = WorldPosition + new Vector3(movement.X, 0, 0);
            if (IsCollidingAt(nextPos))
            {
                movement.X = 0;
            }

            // 2. CHECA EIXO Y
            nextPos = WorldPosition + new Vector3(0, movement.Y, 0);
            if (IsCollidingAt(nextPos))
            {
                movement.Y = 0;
            }

            // 3. Aplica movimento (agora corrigido)
            WorldPosition += new Vector3(movement.X, movement.Y, 0);

            // 4. Atualiza Posição da Tela
            UpdateScreenPosition();

            // 5. Atualiza Velocidade (para referência futura, se necessário)
            WorldVelocity = (dt > 0) ? new Vector2(movement.X / dt, movement.Y / dt) : Vector2.Zero;

            // --- FIM DA LÓGICA DE COLISÃO ---

            if (_firing && (gameTime.TotalGameTime.TotalSeconds - _lastShot > _shotDelay))
            {
                Fire(gameTime, worldAimDirection); // Atira na direção da mira
            }
        }
        // --- FIM DA MODIFICAÇÃO 5 ---

        private bool IsCollidingAt(Vector3 futurePosition)
        {
            // Pega a posição Z base do player
            float baseZ = futurePosition.Z;

            // Calcula os 4 cantos do Bounding Box do player na posição futura
            Vector2 posXY = new Vector2(futurePosition.X, futurePosition.Y);
            Vector2 topLeft = posXY + new Vector2(-_collisionRadius, -_collisionRadius);
            Vector2 topRight = posXY + new Vector2(_collisionRadius, -_collisionRadius);
            Vector2 bottomLeft = posXY + new Vector2(-_collisionRadius, _collisionRadius);
            Vector2 bottomRight = posXY + new Vector2(_collisionRadius, _collisionRadius);

            // Arredonda os 4 cantos para as células do grid
            Vector3 cellTL = new Vector3(MathF.Round(topLeft.X), MathF.Round(topLeft.Y), baseZ);
            Vector3 cellTR = new Vector3(MathF.Round(topRight.X), MathF.Round(topRight.Y), baseZ);
            Vector3 cellBL = new Vector3(MathF.Round(bottomLeft.X), MathF.Round(bottomLeft.Y), baseZ);
            Vector3 cellBR = new Vector3(MathF.Round(bottomRight.X), MathF.Round(bottomRight.Y), baseZ);

            // Checa colisão para cada canto no Z atual e Z+1
            if (GameEngine.SolidTiles.ContainsKey(cellTL) || GameEngine.SolidTiles.ContainsKey(cellTL + new Vector3(0, 0, 1)) ||
        GameEngine.SolidTiles.ContainsKey(cellTR) || GameEngine.SolidTiles.ContainsKey(cellTR + new Vector3(0, 0, 1)) ||
        GameEngine.SolidTiles.ContainsKey(cellBL) || GameEngine.SolidTiles.ContainsKey(cellBL + new Vector3(0, 0, 1)) ||
        GameEngine.SolidTiles.ContainsKey(cellBR) || GameEngine.SolidTiles.ContainsKey(cellBR + new Vector3(0, 0, 1)))
            {
                return true; // Colisão encontrada
            }

            return false; // Sem colisão
        }


        public void TakeDamage(GameTime gameTime)
        {
            double totalMilliseconds = gameTime.TotalGameTime.TotalMilliseconds;
            if (!_isInvincible)
            {
                Life -= 1;
                _lastHit = totalMilliseconds;
                _isInvincible = true;
                if (Texture != null)
                    ExplosionEffect.Create(this.ScreenPosition.X, this.ScreenPosition.Y - (this.Texture.Height / 2f), Constants.PlayerColorGreen, speed: -5);
                GameEngine.Assets.Sounds["hit"].Play();
                GameEngine.ScreenShake = 15;

                // Erro de digitação "t" removido daqui
                if (Life <= 0)
                {
                    Kill();
                }
            }
        }


        public override void Draw(SpriteBatch spriteBatch)
        {
            // --- INÍCIO DA CORREÇÃO ---

            Color tint = Color.White; // 1. Começa com a cor padrão (opaco)

            if (_isInvincible && !IsRemoved)
            {
                float flicker = (float)DateTime.Now.TimeOfDay.TotalMilliseconds / 100f;

                if ((int)flicker % 2 == 0)
                {
                    // 2. Em vez de 'return', nós mudamos a cor para semi-transparente
                    tint = Color.White * 0.5f; // 50% de transparência
                }
                // 3. Se o flicker for ímpar, 'tint' continua Color.White (opaco)

                // 4. A linha "return;" FOI REMOVIDA
            }

            // --- FIM DA CORREÇÃO ---


            Vector2 drawPosition = new Vector2(
        MathF.Round(ScreenPosition.X),
        MathF.Round(ScreenPosition.Y)
      );

            // Lógica de profundidade (está correta como fizemos)
            float baseDepth = IsoMath.GetDepth(WorldPosition);
            const float zLayerBias = 0.001f;
            const float entityBias = 0.0001f;
            float finalDepth = baseDepth - (WorldPosition.Z * zLayerBias) - entityBias;
            finalDepth = MathHelper.Clamp(finalDepth, 0f, 1f);

            spriteBatch.Draw(Texture,
      // Erro de digitação "t" removido daqui
                      drawPosition,
              null,
              tint, // 5. Usa a variável 'tint' (que estará ou opaca ou transparente)
                      0f,
              Origin,
              1.0f,
      // Erro de digitação "D" removido daqui
                      SpriteEffects.None,
              finalDepth);

            ExplosionEffect.Draw(spriteBatch);
        }
    }
}