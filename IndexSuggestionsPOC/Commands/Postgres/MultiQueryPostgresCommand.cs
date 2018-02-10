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
    class MultiQueryPostgresCommand : ChainableCommand
    {
        private readonly MultiQueryPostgresContext context;
        public MultiQueryPostgresCommand(MultiQueryPostgresContext context)
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
                    int datasetIndex = 0;
                    using (var reader = command.ExecuteReader())
                    {
                        do
                        {
                            context.Records.Add(datasetIndex, new List<dynamic>());
                            var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                            foreach (IDataRecord record in reader as IEnumerable)
                            {
                                var expando = new ExpandoObject() as IDictionary<string, object>;
                                foreach (var name in names)
                                {
                                    expando[name] = record[name];
                                }
                                context.Records[datasetIndex].Add(expando);
                            }
                            datasetIndex += 1;
                        } while (reader.NextResult());
                    }
                }
            }
        }
    }
}
