using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace roadie.Library.Setttings
{
    [Serializable]
    public class Thumbnails
    {
        public short Height { get; set; }
        public short Width { get; set; }

        public Thumbnails()
        {
            this.Height = 80;
            this.Width = 80;
        }
    }
}
