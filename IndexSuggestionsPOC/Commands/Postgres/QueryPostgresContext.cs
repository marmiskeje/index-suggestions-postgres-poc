using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class QueryPostgresContext
    {
        public string Query { get; set; }
        public List<dynamic> Records { get; private set; }

        public QueryPostgresContext()
        {
            Records = new List<dynamic>();
        }
    }
}
