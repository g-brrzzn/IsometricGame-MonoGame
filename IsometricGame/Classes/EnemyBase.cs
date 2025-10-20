using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using IsometricGame.Classes.Particles;
using System.Collections.Generic;
using System;

// --- ADIÇÃO 1: Importar o namespace do Pathfinder ---
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

        // --- ADIÇÃO 2: Variáveis para controlar o Pathfinding ---
        protected List<Vector3> _currentPath;
        protected float _pathTimer = 0f;
        // Recalcula o caminho a cada 1 segundo (ou se o caminho acabar)
        protected const float _pathRefreshTime = 1.0f;
        // Quão perto o inimigo precisa chegar de um nó do caminho para avançar para o próximo
        protected const float _nodeReachedThreshold = 0.5f;
        // --- FIM DA ADIÇÃO 2 ---

        public EnemyBase(Vector3 worldPos, List<string> spriteKeys) : base(null, worldPos)
        {
            _sprites = LoadSprites(spriteKeys);
            _explosion = new Explosion();

            if (_sprites.Count > 0 && _sprites.ContainsKey(_currentDirection))
            {
                UpdateTexture(_sprites[_currentDirection]);
                if (Texture != null)
                    // IMPORTANTE: A Origem (pés) é vital para a renderização correta
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
            _isHit = true; if (Texture != null)
                _explosion.Create(this.ScreenPosition.X, this.ScreenPosition.Y - Texture.Height / 2f);

            Life -= 1;
            if (Life <= 0)
            {
                GameEngine.ScreenShake = Math.Max(GameEngine.ScreenShake, 5);
                Kill();
            }
        }

        protected virtual void Shoot()
        {
            if (GameEngine.Player == null || GameEngine.Player.IsRemoved) return;

            for (int i = 0; i < Weight; i++)
            {
                if (GameEngine.Random.Next(0, 500 / Weight) < 1)
                {
                    Vector2 currentPosXY = new Vector2(this.WorldPosition.X, this.WorldPosition.Y);
                    Vector2 playerPosXY = new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y);
                    Vector2 direction = playerPosXY - currentPosXY;
                    if (direction != Vector2.Zero)
                        direction.Normalize();

                    var bullets = Bullet.CreateBullets(
                      pattern: "single",
                      worldPos: currentPosXY,
                      worldDirection: direction,
                      isFromPlayer: false
                    );

                    foreach (var bullet in bullets)
                    {
                        GameEngine.EnemyBullets.Add(bullet);
                        GameEngine.AllSprites.Add(bullet);
                    }
                }
            }
        }

        // --- MODIFICAÇÃO 3: Lógica de Movimento (Move) ---
        // A lógica de "ir reto" foi substituída pela lógica de "seguir o caminho"
        public virtual void Move(GameTime gameTime, float dt)
        {
            if (GameEngine.Player == null || GameEngine.Player.IsRemoved)
            {
                WorldVelocity = Vector2.Zero;
                _currentPath = null;
                return;
            }

            _pathTimer -= dt;

            // 1. Hora de recalcular o caminho?
            if (_pathTimer <= 0f || _currentPath == null || _currentPath.Count == 0)
            {
                _pathTimer = _pathRefreshTime;

                // Arredonda as posições para o grid do pathfinder
                Vector3 start = new Vector3(MathF.Round(WorldPosition.X), MathF.Round(WorldPosition.Y), WorldPosition.Z);
                Vector3 target = new Vector3(MathF.Round(GameEngine.Player.WorldPosition.X), MathF.Round(GameEngine.Player.WorldPosition.Y), GameEngine.Player.WorldPosition.Z);

                // Evita tentar encontrar um caminho para dentro de um tile sólido
                if (GameEngine.SolidTiles.ContainsKey(target))
                {
                    _currentPath = null;
                }
                else
                {
                    _currentPath = Pathfinder.FindPath(start, target);
                }
            }

            Vector2 direction = Vector2.Zero;

            // 2. Segue o caminho
            if (_currentPath != null && _currentPath.Count > 0)
            {
                Vector3 targetNode = _currentPath[0];
                Vector2 currentPosXY = new Vector2(WorldPosition.X, WorldPosition.Y);
                Vector2 targetNodeXY = new Vector2(targetNode.X, targetNode.Y);

                float distance = Vector2.Distance(currentPosXY, targetNodeXY);

                // 3. Chegou perto o suficiente do nó?
                if (distance < _nodeReachedThreshold)
                {
                    _currentPath.RemoveAt(0); // Avança para o próximo nó
                    if (_currentPath.Count == 0)
                    {
                        WorldVelocity = Vector2.Zero; // Chegou ao fim do caminho
                    }
                }

                // 4. Move-se em direção ao nó atual
                if (_currentPath.Count > 0)
                {
                    direction = targetNodeXY - currentPosXY;
                    if (direction != Vector2.Zero)
                    {
                        direction.Normalize();
                    }
                    WorldVelocity = direction * Speed;
                }
            }
            else
            {
                // Sem caminho (jogador inacessível ou já chegou)
                WorldVelocity = Vector2.Zero;
            }

            // --- Fim da Lógica de Pathfinding ---

            if (Math.Abs(direction.X) > Math.Abs(direction.Y))
            {
                _currentDirection = direction.X > 0 ? "south" : "west";
            }
            else if (direction.LengthSquared() > 0)
            {
                _currentDirection = direction.Y > 0 ? "south" : "west";
            }

            if (_sprites.ContainsKey(_currentDirection))
            {
                UpdateTexture(_sprites[_currentDirection]);
                if (Texture != null)
                    Origin = new Vector2(Texture.Width / 2f, Texture.Height);
            }

        }
        // --- FIM DA MODIFICAÇÃO 3 ---

        public override void Update(GameTime gameTime, float dt)
        {
            Move(gameTime, dt); // Define a WorldVelocity desejada (agora via A*)
            Shoot();

            // --- INÍCIO DA LÓGICA DE COLISÃO AABB vs GRID ---
            // (Esta seção permanece IDÊNTICA. Ela é o "corpo" do inimigo
            // que impede o pathfinding de atravessar paredes)

            Vector2 movement = WorldVelocity * dt;

            // 1. CHECA EIXO X
            Vector3 nextPos = WorldPosition + new Vector3(movement.X, 0, 0);
            if (IsCollidingAt(nextPos)) // Usa o novo método auxiliar
            {
                movement.X = 0;
            }

            // 2. CHECA EIXO Y
            nextPos = WorldPosition + new Vector3(0, movement.Y, 0);
            if (IsCollidingAt(nextPos)) // Usa o novo método auxiliar
            {
                movement.Y = 0;
            }

            // 3. Aplica movimento (corrigido)
            // (NÃO usamos mais base.Update() para movimento)
            WorldPosition += new Vector3(movement.X, movement.Y, 0);

            // 4. Atualiza Posição da Tela
            UpdateScreenPosition();

            // 5. Atualiza Velocidade (para referência futura, se necessário)
            WorldVelocity = (dt > 0) ? new Vector2(movement.X / dt, movement.Y / dt) : Vector2.Zero;

            // --- FIM DA LÓGICA DE COLISÃO ---

            _explosion.Update(dt);
            if (_isHit && gameTime.TotalGameTime.TotalMilliseconds - _lastHit > _hitFlashDuration)
            {
                _isHit = false;
            }

            // base.Update() foi removido
        }

        // --- MÉTODO AUXILIAR DE COLISÃO (NOVO) ---
        private bool IsCollidingAt(Vector3 futurePosition)
        {
            float baseZ = futurePosition.Z;

            Vector2 posXY = new Vector2(futurePosition.X, futurePosition.Y);
            Vector2 topLeft = posXY + new Vector2(-_collisionRadius, -_collisionRadius);
            Vector2 topRight = posXY + new Vector2(_collisionRadius, -_collisionRadius);
            Vector2 bottomLeft = posXY + new Vector2(-_collisionRadius, _collisionRadius);
            Vector2 bottomRight = posXY + new Vector2(_collisionRadius, _collisionRadius);

            Vector3 cellTL = new Vector3(MathF.Round(topLeft.X), MathF.Round(topLeft.Y), baseZ);
            Vector3 cellTR = new Vector3(MathF.Round(topRight.X), MathF.Round(topRight.Y), baseZ);
            Vector3 cellBL = new Vector3(MathF.Round(bottomLeft.X), MathF.Round(bottomLeft.Y), baseZ);
            Vector3 cellBR = new Vector3(MathF.Round(bottomRight.X), MathF.Round(bottomRight.Y), baseZ);

            if (GameEngine.SolidTiles.ContainsKey(cellTL) || GameEngine.SolidTiles.ContainsKey(cellTL + new Vector3(0, 0, 1)) ||
              GameEngine.SolidTiles.ContainsKey(cellTR) || GameEngine.SolidTiles.ContainsKey(cellTR + new Vector3(0, 0, 1)) ||
              GameEngine.SolidTiles.ContainsKey(cellBL) || GameEngine.SolidTiles.ContainsKey(cellBL + new Vector3(0, 0, 1)) ||
              GameEngine.SolidTiles.ContainsKey(cellBR) || GameEngine.SolidTiles.ContainsKey(cellBR + new Vector3(0, 0, 1)))
            {
                return true;
            }
            return false;
        }

        // --- AJUSTE NO DRAW PARA RENDERIZAR NA FRENTE ---
        public override void Draw(SpriteBatch spriteBatch)
        {
            Color tint = _isHit ? Color.Red : Color.White;
            if (Texture != null && !IsRemoved)
            {
                Vector2 drawPosition = new Vector2(
                  MathF.Round(ScreenPosition.X),
                  MathF.Round(ScreenPosition.Y)
                );

                // --- INÍCIO DA CORREÇÃO DE FLICKERING ---
                float baseDepth = IsoMath.GetDepth(WorldPosition);
                const float zLayerBias = 0.001f;
                const float entityBias = 0.0001f;

                float finalDepth = baseDepth - (WorldPosition.Z * zLayerBias) - entityBias;
                finalDepth = MathHelper.Clamp(finalDepth, 0f, 1f);
                // --- FIM DA CORREÇÃO ---

                spriteBatch.Draw(Texture,
          drawPosition,
          null,
          tint,
          0f,
          Origin, // A origem nos "pés" (definida no construtor) está CORRETA.
                    1.0f,
          SpriteEffects.None,
          finalDepth); // Usa a profundidade final corrigida
            }

            _explosion.Draw(spriteBatch);
        }
    }
}