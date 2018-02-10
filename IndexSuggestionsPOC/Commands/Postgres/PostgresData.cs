using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class PostgresData
    {
        private static readonly Lazy<PostgresData> instance = new Lazy<PostgresData>(() => new PostgresData());

        public static PostgresData Instance
        {
            get { return instance.Value; }
        }

        public List<dynamic> PgClasses { get; set; }
        public List<dynamic> PgNamespaces { get; set; }
        public List<dynamic> PgOperators { get; set; }
        public List<dynamic> PgTypes { get; set; }
        public List<dynamic> PgAttributes { get; set; }
        public List<dynamic> Indices { get; set; }

        private PostgresData()
        {

        }
    }
}
