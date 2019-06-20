using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonRank.UI
{
    public class SearchModel
    {
        public string Link { get; set; }

        public string KeyWord { get; set; }

        public string Asin { get; set; }

        public int Page { get; set; } = 1;

        public int TotalPage { get; set; } = 1;

        public int Rank { get; set; } = 0;

        public SearchResult SResult { get; set; } = null;
    }

    public class SearchResult
    {
        public string Position { get; set; } = string.Empty;

        public bool IsSponsored { get; set; } = false;

        public string DetailLink { get; set; } = string.Empty;
    }
}
