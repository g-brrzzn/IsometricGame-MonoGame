using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace IsometricGame.Classes
{
    public class Sprite
    {
        public Texture2D Texture { get; protected set; }
        public Vector3 WorldPosition { get; set; }
        public Vector2 WorldVelocity { get; set; }
        public Vector2 ScreenPosition { get; protected set; }
        public bool IsRemoved { get; set; } = false;
        public Vector2 Origin { get; protected set; }


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
                Origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            }
        }


        public void UpdateScreenPosition()        {
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


            float baseDepth = IsoMath.GetDepth(WorldPosition);

            const float zLayerBias = 0.001f;

            float finalDepth = baseDepth - (WorldPosition.Z * zLayerBias);

            finalDepth = MathHelper.Clamp(finalDepth, 0f, 1f);


            spriteBatch.Draw(Texture,
                drawPosition,
                null,
                Color.White,
                0f,
                Origin,                1.0f,
                SpriteEffects.None,
                finalDepth);        }


        public void Kill() => IsRemoved = true;
    }
}