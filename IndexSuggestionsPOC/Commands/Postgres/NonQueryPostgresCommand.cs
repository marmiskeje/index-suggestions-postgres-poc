using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class NonQueryPostgresCommand : ChainableCommand
    {
        private readonly QueryPostgresContext context;
        public NonQueryPostgresCommand(QueryPostgresContext context)
        {
            this.context = context;
        }
        protected override void OnExecute()
        {
            
            using (NpgsqlConnection connection = new NpgsqlConnection("Server=127.0.0.1;Port=5432;Database=test;User Id=postgres;Password = root; "))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = context.Query;
                    command.CommandType = CommandType.Text;
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        context.Records.Add(result);
                    }
                }
            }
        }
    }
}
