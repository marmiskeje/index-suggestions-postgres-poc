using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class MultiQueryPostgresContext
    {
        public string Query { get; set; }
        public Dictionary<int, List<dynamic>> Records { get; private set; }

        public MultiQueryPostgresContext()
        {
            Records = new Dictionary<int, List<dynamic>>();
        }
    }
}
