using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace IsometricGame.Classes
{
    // --- Patched base Sprite ---
    public class Sprite
    {
        public Texture2D Texture { get; protected set; }
        public Vector3 WorldPosition { get; set; }
        public Vector2 WorldVelocity { get; set; }
        public Vector2 ScreenPosition { get; protected set; }
        public bool IsRemoved { get; set; } = false;
        public Vector2 Origin { get; protected set; }


        // Distance (in WORLD units) from WorldPosition.Y to the visual "feet"/anchor used for depth.
        // Default 0 -> WorldPosition already represents the feet.
        public float BaseYOffsetWorld { get; set; } = 0f;


        public Sprite(Texture2D texture, Vector3 worldPosition)
        {
            WorldPosition = worldPosition;
            WorldVelocity = Vector2.Zero;
            UpdateTexture(texture);
            UpdateScreenPosition();
        }


        protected void UpdateTexture(Texture2D newTexture)
        {
            if (newTexture != null)
            {
                Texture = newTexture;
                // origin default is center; specific sprites (player, tiles) can override in their constructors
                Origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            }
        }


        protected void UpdateScreenPosition()
        {
            ScreenPosition = IsoMath.WorldToScreen(WorldPosition);
        }


        public virtual void Update(GameTime gameTime, float dt)
        {
            WorldPosition += new Vector3(WorldVelocity.X, WorldVelocity.Y, 0) * dt;
            UpdateScreenPosition();
        }


        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (Texture == null || IsRemoved) return;

            Vector2 drawPosition = new Vector2(MathF.Round(ScreenPosition.X), MathF.Round(ScreenPosition.Y));

            // --- INÍCIO DA CORREÇÃO DE FLICKERING ---

            // 1. Calcula a profundidade base (que vem do seu IsoMath, baseado em X+Y)
            float baseDepth = IsoMath.GetDepth(WorldPosition);

            // 2. Define um "desvio" (bias) para cada camada Z.
            //    Este valor DEVE ser menor que a menor diferença de profundidade entre dois tiles
            //    (que é 1.0f / (WorldSize.X + WorldSize.Y), ou seja, 1/200 = 0.005)
            //    Usar 0.001f é seguro.
            const float zLayerBias = 0.001f;

            // 3. Aplica o bias.
            //    GetDepth() nos dá 1.0 (trás) e 0.0 (frente).
            //    Um Z maior (Z=1, Z=2) está "acima" e deve ser desenhado "na frente" do Z=0.
            //    "Na frente" significa um valor de profundidade MENOR (mais perto de 0.0).
            //    Portanto, SUBTRAÍMOS o Z.
            float finalDepth = baseDepth - (WorldPosition.Z * zLayerBias);

            // 4. Garante que o valor final esteja entre 0.0 e 1.0
            finalDepth = MathHelper.Clamp(finalDepth, 0f, 1f);

            // --- FIM DA CORREÇÃO ---

            spriteBatch.Draw(Texture,
                drawPosition,
                null,
                Color.White,
                0f,
                Origin, // A origem (centro) padrão do Sprite.cs está OK para tiles.
                1.0f,
                SpriteEffects.None,
                finalDepth); // Usa a profundidade final corrigida
        }


        public void Kill() => IsRemoved = true;
    }
}