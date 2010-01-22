using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xna_WindowForm
{
    public class Tweet
    {
        private String id;
        public String Id { get { return id; } set { id = value; } }

        private String text;
        public String Text { get { return text; } set { text = value; } }

        private DateTime createdAt;
        public DateTime CreatedAt { get { return createdAt; } set { createdAt = value; } }

        public Tweet( String id, String text, String createdAt)
        {
            this.id = id;
            this.text = text;

            String format = "ddd MMM d HH':'mm':'ss zzz yyyy"; // Tue Nov 17 14:55:09 +0000 2009
            this.createdAt = DateTime.ParseExact( createdAt, format, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.None);
        }

        public override bool Equals(object obj)
        {
            //objがnullか、型が違うときは、等価でない
            if (obj == null || this.GetType() != obj.GetType())
            {
                return false;
            }

            if (((Tweet)obj).id.Equals(this.id))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
