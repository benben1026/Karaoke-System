using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* I guess it will be useful to define a class to gather essential info of a piece of music
 * I guess the UI and database can make use of it.
*/

namespace P2P_Karaoke_System {
    class MusicData {
        public string Filename { get; set; }
        public string Title { get; set; }
        public string Singer { get; set; }
        public string Album { get; set; }

        public MusicData(String filename, String title, String singer, String album) {
            this.Filename = filename;
            this.Title = title;
            this.Singer = singer;
            this.Album = album;
        }
    }
}
