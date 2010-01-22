using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using FarseerGames.FarseerPhysics;
using FarseerGames.FarseerPhysics.Collisions;
using FarseerGames.FarseerPhysics.Dynamics;
using FarseerGames.FarseerPhysics.Factories;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Xna_WindowForm
{
    class UserLoader
    {
        private Game1 parent;
        private Form_user fu;

        public UserLoader( Game1 parent, Form_user fu){
            this.parent = parent;
            this.fu = fu;
        }

        public void Run()
        {
            try
            {
                fu.LoadFvo(parent.GraphicsDevice, parent.Content);

                if (!parent.isUsingObjects)
                {
                    parent.objects.Add(fu);
                }
                else
                {
                    fu.Dispose();
                }
            }
            catch
            {
                Console.WriteLine( "* error in loading visual object.");
            }
        }
    }
}
