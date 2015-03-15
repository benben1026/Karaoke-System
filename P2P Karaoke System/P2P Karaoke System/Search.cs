using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_Karaoke_System
{
    class SearchRequest
    {
        private string keyword { get; set; }

        public SearchRequest(string keyword)
        {
            this.keyword = keyword;
        }
    }

    class SearchResponse
    {
        private MusicCopy[] result { get; set; }

        public SearchResponse(MusicCopy[] data)
        {
            this.result = data;
        }
    }
}
