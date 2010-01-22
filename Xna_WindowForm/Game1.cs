using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using FarseerGames.FarseerPhysics;
using FarseerGames.FarseerPhysics.Collisions;
using FarseerGames.FarseerPhysics.Dynamics;
using FarseerGames.FarseerPhysics.Factories;

using Yedda;

using ImageUtils;

namespace Xna_WindowForm
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        //-- test data start --//
        String testImagePath = @"C:\Documents and Settings\[***]\My Documents\My Pictures\kurobuta.jpg";
        //-- test data end --//

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public static int SCREEN_WIDTH = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        public static int SCREEN_HEIGHT = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

        // windows
        public Form xnaForm;
        public static float SCALE = 1f;

        // world variables
        PhysicsSimulator world;
        private Body groundBody;
        private Geom groundGeom;
        private Vertices groundVertices;

        public bool isUsingObjects = false;
        public List<Form_user> objects = new List<Form_user>();

        //
        public static String IMG_CACHE_DIR = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Magic Briefcase\testData\twitterFallCache\";
        public static String DEFAULT_IMG = "default.png";

        
        int timeSinceLastGetTweet = 0;
        int getTweetInterval = 20000;

        int timeSinceLastAddUser = 0;
        int addUserInterval = 5000;

        private TweetRetriever tRet;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = (int)(SCREEN_WIDTH * SCALE);
            graphics.PreferredBackBufferHeight = (int)(SCREEN_HEIGHT * SCALE);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            //-- setup windows
            xnaForm = Form.FromHandle(this.Window.Handle) as System.Windows.Forms.Form;
            //xnaForm.TopMost = true;
            xnaForm.Location = new System.Drawing.Point(0, Screen.PrimaryScreen.Bounds.Height - xnaForm.Height);

            xnaForm.WindowState = FormWindowState.Maximized;
            xnaForm.ShowInTaskbar = false;
            xnaForm.TransparencyKey = System.Drawing.Color.White;
            xnaForm.FormBorderStyle = FormBorderStyle.None;

            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.BackColor = System.Drawing.Color.White;
            xnaForm.Controls.Add(p);

            //-- 
            world = new PhysicsSimulator(new Vector2(0, 3f * SCALE));

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // 最初のユーザリスト取得
            tRet = new TweetRetriever(this);
            Thread t = new Thread(new ThreadStart(tRet.Run));
            t.Start();

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            // ground
            groundVertices = new Vertices(new Vector2[] {
					new Vector2((-Window.ClientBounds.Width / 2) * SCALE, 0  * SCALE),
					new Vector2((Window.ClientBounds.Width / 2)  * SCALE, 0  * SCALE),
					new Vector2((Window.ClientBounds.Width / 2)  * SCALE, 10 * SCALE),
                    new Vector2((-Window.ClientBounds.Width / 2) * SCALE, 10 * SCALE),
	        });
            groundBody = BodyFactory.Instance.CreatePolygonBody(world, groundVertices, 1f);
            groundBody.IsStatic = true;
            groundBody.Position = new Vector2( (Window.ClientBounds.Width / 2) * SCALE, (Window.ClientBounds.Height - 150) * SCALE);
            groundGeom = GeomFactory.Instance.CreateRectangleGeom(groundBody, Window.ClientBounds.Width, 3);
            groundGeom.FrictionCoefficient = 0.7f;
            world.Add(groundBody);
            world.Add(groundGeom);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        Random r = new Random();
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            world.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            #region load a friend
            timeSinceLastAddUser += gameTime.ElapsedGameTime.Milliseconds;
            if (timeSinceLastAddUser > addUserInterval)
            {
                timeSinceLastAddUser -= addUserInterval;
                
                // キューから未描画のユーザを取得して描画
                List<User> friends = tRet.Friends;
                bool add = false;
                foreach (var f in friends)
                {
                    Form_user friend = new Form_user(world, new Vector2((float)(r.Next((int)(SCREEN_WIDTH * 0.7f * SCALE), (int)(SCREEN_WIDTH * 0.95f * SCALE))), 0), f);
                    if (objects.Contains(friend))
                    {
                        Form_user _friend = objects[objects.IndexOf(friend)];

                        if (!_friend.Visible)
                        {
                            xnaForm.AddOwnedForm(_friend);
                            _friend.Show();
                        }

                        if (_friend.Location.X < -_friend.Width - 50 || _friend.Location.X > Game1.SCREEN_WIDTH + _friend.Width + 50
                            || _friend.Location.Y > Game1.SCREEN_HEIGHT + _friend.Height + 50)
                        {   // y < - this.Height はしない。これすると上から落とす時不都合
                            _friend.Close();
                            objects.Remove(_friend);
                        }
                        else
                        {
                            // Console.WriteLine("*** apply impulse? :{0} ({1})", !f.LatestTweetDispFlag, f.ToString());
                            if (!f.LatestTweetDispFlag)
                            {
                                // _friend.Body.ApplyImpulse(new Vector2(100, 100));
                                // _friend.Body.ApplyForce(new Vector2(100, 100));
                                _friend.jump( new Vector2( 0, -500));
                                f.LatestTweetDispFlag = true;   // 表示済にする
                            }

                            if (friend.User.Tweets.Count > _friend.User.Tweets.Count)
                            {
                                // _friend.resize(1.05f);
                            }
                            else
                            {
                                // _friend.resize(0.95f);
                            }
                        }
                    }

                    if ( !objects.Contains(friend) && !add)
                    {
                        UserLoader ul = new UserLoader( this, friend);
                        Thread t = new Thread(new ThreadStart(ul.Run));
                        t.Start();

                        add = true;
                    }             
                }
            }
            #endregion

            #region update friend list
            timeSinceLastGetTweet += gameTime.ElapsedGameTime.Milliseconds;
            if (timeSinceLastGetTweet > getTweetInterval)
            {
                timeSinceLastGetTweet -= getTweetInterval;

                // 発言のあったお友達リストを別スレッドでアップデート
                tRet = new TweetRetriever(this);
                Thread t = new Thread(new ThreadStart(tRet.Run));
                t.Start();
            }
            #endregion

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend);

            isUsingObjects = true;
            foreach (Form_user fvo in objects)
            {
                fvo.Draw(spriteBatch);
                fvo.Draw();
            }
            isUsingObjects = false;

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
