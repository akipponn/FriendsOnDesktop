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
    public partial class Form_user : Form
    {
        private PhysicsSimulator world;

        private Body body;
        public Body Body { get { return body; } }

        private Geom geom;
        private Vertices vertices;
        private Vector2 position;

        private User user;
        public User User { get { return user; } }

        public Form_user( PhysicsSimulator world, Vector2 position, User user)
        {
            this.world = world;
            this.position = position;
            this.user = user;

            InitializeComponent();
        }

        private void Form_visualObject_Load(object sender, EventArgs e)
        {
        }

        public void LoadFvo(GraphicsDevice gd, ContentManager content)
        {
            try
            {
                this.pictureBox1.Image = Image.FromFile(user.ProfileImageCachePath);

                vertices = new Vertices(new Vector2[] {
					new Vector2(0, 0),
					new Vector2(this.Size.Width * Game1.SCALE, 0),
					new Vector2(this.Size.Width * Game1.SCALE, this.Size.Height * Game1.SCALE),
                    new Vector2(0, this.Size.Height * Game1.SCALE),
	            });
                vertices.SubDivideEdges(0.5f);
                body = BodyFactory.Instance.CreatePolygonBody(world, vertices, 20f);

                body.ApplyImpulse(new Vector2(0, 100));

                geom = new Geom(body, vertices, 1);
                geom.FrictionCoefficient = 1f;
                body.Position = this.position;

                world.Add(body);
                world.Add(geom);

                this.Location = new System.Drawing.Point((int)(body.Position.X), (int)(body.Position.Y));
            }
            catch
            {
                throw new Exception();
            }
        }

        public void resize( float scale)
        {
            Image dest = ImageUtils.Resize.resizeImageWithScale(this.pictureBox1.Image, scale);
            this.pictureBox1.Image = dest;
            this.Size = dest.Size;

            dest.Dispose();
        }

        public void Draw(SpriteBatch sb)
        {
            // for debug
            // sb.Draw( texture, body.Position, null, Microsoft.Xna.Framework.Graphics.Color.White, body.Rotation, vertices.GetCentroid(), 1.0f, SpriteEffects.None, 0.5f);
        }

        private delegate void SetLocationCallBack();
        public void Draw()
        {
            if (dragged)
            {
                // ドラッグの間は描画しなくてOK
            }
            else
            {
                int x = (int)(body.Position.X / Game1.SCALE);
                int y = (int)(body.Position.Y / Game1.SCALE);
                this.Location = new System.Drawing.Point(x, y);
            }
        }

        public void jump( Vector2 v)
        {
            body.ApplyImpulse(v);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (!dragged)
            {
                body.IsStatic = !body.IsStatic;
            }
            else
            {
                body.Position = new Vector2(this.Location.X * Game1.SCALE, this.Location.Y * Game1.SCALE);
                dragged = false;
            }
        }

        public override bool Equals(object obj)
        {
            //objがnullか、型が違うときは、等価でない
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            if (((Form_user)obj).User.Id.Equals(this.user.Id))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #region ドラッグ&ドロップの処理
        private System.Drawing.Point mousePoint;
        private System.Drawing.Point prevMousePoint;
        protected void mouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.BringToFront();
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                mousePoint = new System.Drawing.Point(e.X, e.Y);
                prevMousePoint = System.Windows.Forms.Cursor.Position;
            }
        }

        private bool dragged = false;
        protected void mouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.Left += e.X - mousePoint.X;
                this.Top += e.Y - mousePoint.Y;

                if (!System.Windows.Forms.Cursor.Position.Equals(prevMousePoint))
                {
                    dragged = true;
                }
                prevMousePoint = System.Windows.Forms.Cursor.Position;
            }
        }

        private void mouseUp(object sender, MouseEventArgs e)
        {

        }
        #endregion
    }
}
