using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonRank.Core
{
    public class CountryModel
    {
        public string CountryName { get; set; }

        public string Link { get; set; }

        public string ZipCode { get; set; }

        public bool isProxy { get; set; }
    }
}
