using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

using Yedda;

using System.Threading;

namespace Xna_WindowForm
{
    class TweetRetriever
    {
        private Game1 parent;
        
        private List<User> friends = new List<User>();
        public List<User> Friends { get { return friends; } }

        public TweetRetriever(Game1 parent)
        {
            this.parent = parent;
        }

        public void Run()
        {
            Console.WriteLine( "new thread to retrieve friends' tweets");

            Twitter twitter = new Twitter();

            // 発言のあったお友達リストをアップデート
            TextReader stringReader = new StringReader(twitter.GetFriendsTimeline("username", "passwd", Twitter.OutputFormatType.XML));
            XDocument xdoc = XDocument.Load(stringReader);

            friends = User.UserList(xdoc, new List<User>());
        }
    }
}
