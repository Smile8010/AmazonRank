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

        /// <summary>
        /// 搜索类型 0 非广告，1 仅广告，2 两者
        /// </summary>
        public int SearchType { get; set; } = 0;

        public bool isFindedAsin
        {
            get
            {
                if (SearchType == 0)
                {
                    return FindModels.Any(o => !o.IsSponsored);
                }
                else if (SearchType == 1)
                {
                    return FindModels.Any(o => o.IsSponsored);
                }
                else
                {
                    return FindModels.Count > 2;
                }
            }
        }

        public string ResultNumString { get; set; } = string.Empty;


        //public string Position { get; set; } = string.Empty;

        //public bool IsSponsored { get; set; } = false;

        //public string DetailLink { get; set; } = string.Empty;

        //public int PosIndex { get; set; } = 0;


        public List<FindModel> FindModels { get; set; } = new List<FindModel>();

        public string ErrorMsg { get; set; } = string.Empty;

        //public SearchResult SResult { get; set; } = null;
    }

    public class FindModel
    {
        public string KeyWord { get; set; } = string.Empty;

        public int Page { get; set; } = 1;

        public int Rank { get; set; } = 0;

        public bool IsSponsored { get; set; } = false;

        public int Pos { get; set; } = 0;

        public string ResultNumString { get; set; } = string.Empty;

        public string Position { get; set; }

        public string IsSponsoredText
        {
            get
            {
                return IsSponsored ? "是" : "否";
            }
        }
    }

    //public class SearchResult
    //{
    //    public string Position { get; set; } = string.Empty;

    //    public bool IsSponsored { get; set; } = false;

    //    public string DetailLink { get; set; } = string.Empty;

    //    public int PosIndex { get; set; } = 0;


    //}
}
