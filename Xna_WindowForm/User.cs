using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
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
    public class User
    {
        private Body body;
        private Geom geom;
        private Vertices vertices;
        private PhysicsSimulator world;
        private Vector2 position;

        private String id;
        public String Id { get { return id; } set { id = value; } }

        private String name;
        public String Name { get { return name; } set { name = value; } }

        private String screenName;
        public String ScreenName { get { return screenName; } set { screenName = value; } }
       
        private String description;
        public String Description { get { return description; } set { description = value; } }

        private String profileImageUrl;
        public String ProfileImageUrl { get { return profileImageUrl; } set { profileImageUrl = value; } }
        private String profileImageCachePath;
        public String ProfileImageCachePath { get { return profileImageCachePath; } set { profileImageCachePath = value; } }

        private String url;
        public String Url { get { return url; } set { url = value; } }

        private DateTime lastStatusUpdate;
        public DateTime LastStatusUpdate { get { return lastStatusUpdate; } set { lastStatusUpdate = value; } }

        private bool latestTweetDispFlag = true;
        public bool LatestTweetDispFlag { get { return latestTweetDispFlag; } set { latestTweetDispFlag = value; } }

        private Tweet latestTweet;
        public Tweet LatestTweet { get { return latestTweet; } set { latestTweet = value; } }

        private List<Tweet> tweets;
        public List<Tweet> Tweets { get { return tweets; } set { tweets = value; } }

        public User(String id, String name, String screenName, String profileImageUrl, String lastStatusUpdate)
        {
            this.id = id;
            this.name = name;
            this.screenName = screenName;
            this.profileImageUrl = profileImageUrl;

            this.lastStatusUpdate = System.DateTime.ParseExact(lastStatusUpdate, "ddd MMM dd HH':'mm':'ss zzz yyyy", System.Globalization.DateTimeFormatInfo.InvariantInfo);

            tweets = new List<Tweet>();
        }

        public static List<User> UserList( XDocument xdoc, List<User> users)
        {
            foreach (var status in xdoc.Root.Elements())
            {
                String tweetId = status.Element("id").Value;
                String text = status.Element("text").Value;
                String createdAt = status.Element("created_at").Value;
                Tweet tweet = new Tweet(tweetId, text, createdAt);
                
                XElement xuser = status.Element("user");
                String userId = xuser.Element("id").Value;
                String lastStatusUpdate = status.Element("created_at").Value; // Thu Dec 10 04:49:37 +0000 2009
                User user = new User( userId, null, null, null, lastStatusUpdate);
                if( users.Contains( user))
                {
                    User u = users[users.IndexOf(user)];
                    u.lastStatusUpdate = user.lastStatusUpdate;
                    u.LatestTweetDispFlag = false;

                    if (u.LatestTweet != null)
                    {
                        u.Tweets.Add(u.LatestTweet);
                    }
                    u.LatestTweet = tweet;
                }
                else
                {
                    user.name = xuser.Element("name").Value;
                    user.screenName = xuser.Element("screen_name").Value;
                    user.profileImageUrl = xuser.Element("profile_image_url").Value;

                    String[] _temp = user.ProfileImageUrl.Split(new char[] { '/' });
                    String ext = (_temp[_temp.Length - 1].Split(new char[] { '.' }))[1];
                    String imgCachePath = Game1.IMG_CACHE_DIR + userId + "." + ext;

                    if (File.Exists(imgCachePath) && ( DateTime.Now - File.GetCreationTime(imgCachePath)).TotalDays < 1)
                    {
                        user.ProfileImageCachePath = imgCachePath;
                    }
                    else
                    {
                        try
                        {
                            if (File.Exists(imgCachePath)) { File.Delete(imgCachePath); }
                            System.Net.WebClient wc = new System.Net.WebClient();
                            String tempImgCachePath = imgCachePath + "_tmp";
                            user.ProfileImageCachePath = imgCachePath;
                            wc.DownloadFile(user.ProfileImageUrl, tempImgCachePath);
                            wc.Dispose();

                            Image src = Image.FromFile(tempImgCachePath);
                            Image dest = ImageUtils.Resize.resizeImage((Bitmap)src, 48, 48);

                            dest.Save(imgCachePath);
                            File.Delete(tempImgCachePath);
                        }
                        catch( Exception ee)
                        {
                            Console.WriteLine( "[{0}] {1}", user.ToString(), ee.StackTrace);
                        }
                    }

                    user.LatestTweet = tweet;

                    users.Add(user);
                }
            }

            return users;
        }

        public override bool Equals(object obj)
        {
            //objがnullか、型が違うときは、等価でない
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            if (((User)obj).id.Equals(this.id))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return this.id + ":" + this.name + ":" + this.screenName + ":" + this.description + ":" + this.profileImageUrl + ":" + tweets.Count;
        }
    }
}
