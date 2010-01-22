using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using FarseerGames.FarseerPhysics;
using FarseerGames.FarseerPhysics.Collisions;
using FarseerGames.FarseerPhysics.Dynamics;
using FarseerGames.FarseerPhysics.Factories;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Xna_WindowForm
{
    class VisualObject : PictureBox
    {
        private Body body;
        private Geom geom;
        private Vertices vertices;
        private PhysicsSimulator world;
        private Vector2 position;

        private string imageFilepath;

        public VisualObject(PhysicsSimulator world, Vector2 position, string imageFilepath)
        {
            this.world = world;
            this.position = position;

            if (!File.Exists(imageFilepath))
            {
                imageFilepath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\My Pictures\kurobut.jpg";
            }
            this.imageFilepath = imageFilepath;
            this.Image = Image.FromFile(imageFilepath);
            this.Size = this.Image.Size;
        }

        public void Load( GraphicsDevice gd, ContentManager content)
        {
            vertices = new Vertices(new Vector2[] {
					new Vector2(0, 0),
					new Vector2(this.Size.Width * Game1.SCALE, 0),
					new Vector2(this.Size.Width * Game1.SCALE, this.Size.Height * Game1.SCALE),
                    new Vector2(0, this.Size.Height * Game1.SCALE),
	        });
            vertices.SubDivideEdges(0.5f);
            body = BodyFactory.Instance.CreatePolygonBody(world, vertices, 1f);
            geom = new Geom(body, vertices, 1);
            geom.FrictionCoefficient = 0.1f;
            body.Position = this.position;

            world.Add(body);
            world.Add(geom);
        }

        public void Load( AppBaseForm form)
        {
            this.Location = new System.Drawing.Point((int)(body.Position.X), (int)(body.Position.Y));
            this.Size = this.Image.Size;
            form.Controls.Add(this);
        }

        public void Draw(SpriteBatch sb)
        {
//            sb.Draw( texture, body.Position, null, Microsoft.Xna.Framework.Graphics.Color.White, body.Rotation, vertices.GetCentroid(), 1.0f, SpriteEffects.None, 0.5f);
        }

        public void Draw()
        {
            // form の位置も update するよ
            this.Location = new System.Drawing.Point((int)(body.Position.X * 10), (int)(body.Position.Y * 10));
        }
    }
}
