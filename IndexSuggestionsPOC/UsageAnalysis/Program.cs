using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class Program
    {
        private static IDatabase redisDB;
        static void Main(string[] args)
        {
            using (var redis = ConnectionMultiplexer.Connect("localhost"))
            {
                redisDB = redis.GetDatabase(0);
                using (var timer = new Timer(GetLatestStatisticsJob, null, 0, 60000))
                {
                    Console.ReadLine();
                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        private static void GetLatestStatisticsJob(object obj)
        {
            MultiQueryPostgresContext context = new MultiQueryPostgresContext();
            context.Query = "select * from pg_stat_user_tables;select * from pg_stat_user_indexes;select * from pg_stat_user_functions; select * from pg_stat_get_snapshot_timestamp(); select * from pg_stat_reset();";
            new MultiQueryPostgresCommand(context).Execute();

            DatabasesUsage data = new DatabasesUsage();
            string strData = redisDB.StringGet("IndexSuggestionsPOC.DatabasesUsage");
            if (!String.IsNullOrEmpty(strData))
            {
                data = JsonConvert.DeserializeObject<DatabasesUsage>(strData);
            }
            data.GeneratedDate = context.Records[3].First().pg_stat_get_snapshot_timestamp;
            DateTime dayToUse = data.GeneratedDate.Date;
            if (!data.DayGenerationExecutions.ContainsKey(dayToUse))
            {
                data.DayGenerationExecutions.Add(dayToUse, 0);
            }
            data.DayGenerationExecutions[dayToUse] += 1;
            foreach (var tableData in context.Records[0])
            {
                string dbName = tableData.schemaname;
                string relName = tableData.relname;
                long seqScansCount = tableData.seq_scan;
                long idxScansCount = tableData.idx_scan;
                if (!data.Databases.ContainsKey(dbName))
                {
                    data.Databases.Add(dbName, new DatabaseUsage());
                }
                var database = data.Databases[dbName];
                if (!database.Tables.ContainsKey(relName))
                {
                    database.Tables.Add(relName, new TableUsage());
                }
                var table = database.Tables[relName];
                if (idxScansCount > 0)
                {
                    if (!table.IdxScans.ContainsKey(dayToUse))
                    {
                        table.IdxScans.Add(dayToUse, 0);
                    }
                    table.IdxScans[dayToUse] += tableData.idx_scan;
                }
                if (seqScansCount > 0)
                {
                    if (!table.SeqScans.ContainsKey(dayToUse))
                    {
                        table.SeqScans.Add(dayToUse, 0);
                    }
                    table.SeqScans[dayToUse] += tableData.seq_scan;
                }
            }
            foreach (var indexData in context.Records[1])
            {
                string dbName = indexData.schemaname;
                string relName = indexData.relname;
                string idxName = indexData.indexrelname;
                long scansCount = indexData.idx_scan;
                if (!data.Databases.ContainsKey(dbName))
                {
                    data.Databases.Add(dbName, new DatabaseUsage());
                }
                var database = data.Databases[dbName];
                if (!database.Tables.ContainsKey(relName))
                {
                    database.Tables.Add(relName, new TableUsage());
                }
                if (scansCount > 0)
                {
                    var table = database.Tables[relName];
                    if (!table.Indices.ContainsKey(idxName))
                    {
                        table.Indices.Add(idxName, new IndexUsage());
                    }
                    var index = table.Indices[idxName];
                    if (!index.IdxScans.ContainsKey(dayToUse))
                    {
                        index.IdxScans.Add(dayToUse, 0);
                    }
                    index.IdxScans[dayToUse] += scansCount;
                }
            }
            foreach (var procedureData in context.Records[2])
            {
                string dbName = procedureData.schemaname;
                string procName = procedureData.funcname;
                long calls = procedureData.calls;
                if (calls > 0)
                {
                    if (!data.Databases.ContainsKey(dbName))
                    {
                        data.Databases.Add(dbName, new DatabaseUsage());
                    }
                    var database = data.Databases[dbName];
                    if (!database.Procedures.ContainsKey(procName))
                    {
                        database.Procedures.Add(procName, new ProcedureUsage());
                    }
                    var procedure = database.Procedures[procName];
                    if (!procedure.Calls.ContainsKey(dayToUse))
                    {
                        procedure.Calls.Add(dayToUse, 0);
                    }
                    procedure.Calls[dayToUse] += calls; 
                }
            }

            strData = JsonConvert.SerializeObject(data, Formatting.Indented);
            redisDB.StringSet("IndexSuggestionsPOC.DatabasesUsage", strData);
            PrintNotUsed(data);
        }

        private static void PrintNotUsed(DatabasesUsage dbUsage)
        {
            string dbName = "test";
            TimeSpan notUsedThreshold = TimeSpan.FromDays(1);
            DateTime atLeastAccessDay = DateTime.Now.Date.Subtract(notUsedThreshold);
            ISet<string> allTables = new HashSet<string>();
            QueryPostgresContext postgresContext = new QueryPostgresContext();
            postgresContext.Query = String.Format("SELECT * FROM pg_catalog.pg_tables where schemaname = '{0}'", dbName);
            new QueryPostgresCommand(postgresContext).Execute();
            foreach (var item in postgresContext.Records)
            {
                allTables.Add(item.tablename);
            }
            var db = dbUsage.Databases[dbName];
            foreach (var tableName in allTables)
            {
                if (!db.Tables.ContainsKey(tableName))
                {
                    if (dbUsage.DayGenerationExecutions.FirstOrDefault().Key <= atLeastAccessDay)
                    {
                        Console.WriteLine("Not used table: {0} since: {1}", tableName, dbUsage.DayGenerationExecutions.FirstOrDefault().Key);
                    }
                }
                else
                {
                    var table = db.Tables[tableName];
                    SortedList<DateTime, long> setToUse = table.IdxScans;
                    if (table.IdxScans.LastOrDefault().Key == default(DateTime))
                    {
                        setToUse = table.SeqScans;
                    }
                    DateTime lastUsedDay = table.IdxScans.LastOrDefault().Key == default(DateTime) ? dbUsage.DayGenerationExecutions.FirstOrDefault().Key : table.IdxScans.LastOrDefault().Key;
                    if (lastUsedDay <= atLeastAccessDay)
                    {
                        Console.WriteLine("Not used table: {0} since: {1}", tableName, lastUsedDay);
                    }

                    ISet<string> allIndices = new HashSet<string>();
                    postgresContext = new QueryPostgresContext();
                    postgresContext.Query = String.Format("SELECT * FROM pg_catalog.pg_indexes where schemaname = '{0}' and tablename = '{1}'", dbName, tableName);
                    new QueryPostgresCommand(postgresContext).Execute();
                    foreach (var item in postgresContext.Records)
                    {
                        allIndices.Add(item.indexname);
                    }
                    foreach (var indexName in allIndices)
                    {
                        if (!table.Indices.ContainsKey(indexName))
                        {
                            if (dbUsage.DayGenerationExecutions.FirstOrDefault().Key <= atLeastAccessDay)
                            {
                                Console.WriteLine("Not used index: {0} since: {1}", indexName, dbUsage.DayGenerationExecutions.FirstOrDefault().Key);
                            }
                        }
                        else
                        {
                            var index = table.Indices[indexName];
                            lastUsedDay = index.IdxScans.LastOrDefault().Key == default(DateTime) ? dbUsage.DayGenerationExecutions.FirstOrDefault().Key : index.IdxScans.LastOrDefault().Key;
                            if (lastUsedDay <= atLeastAccessDay)
                            {
                                Console.WriteLine("Not used index: {0} since: {1}", indexName, lastUsedDay);
                            }
                        }
                    }
                }
            }
            ISet<string> allProcedures = new HashSet<String>();
            postgresContext = new QueryPostgresContext();
            postgresContext.Query = String.Format("SELECT n.nspname, p.proname FROM pg_catalog.pg_proc p INNER JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace WHERE n.nspname = '{0}'", dbName);
            new QueryPostgresCommand(postgresContext).Execute();
            foreach (var item in postgresContext.Records)
            {
                allProcedures.Add(item.proname);
            }
            foreach (var procName in allProcedures)
            {
                if (!db.Procedures.ContainsKey(procName))
                {
                    if (dbUsage.DayGenerationExecutions.FirstOrDefault().Key <= atLeastAccessDay)
                    {
                        Console.WriteLine("Not used proc: {0} since: {1}", procName, dbUsage.DayGenerationExecutions.FirstOrDefault().Key);
                    }
                }
                else
                {
                    var proc = db.Procedures[procName];
                    DateTime lastUsedDay = proc.Calls.LastOrDefault().Key == default(DateTime) ? dbUsage.DayGenerationExecutions.FirstOrDefault().Key : proc.Calls.LastOrDefault().Key;
                    if (lastUsedDay <= atLeastAccessDay)
                    {
                        Console.WriteLine("Not used proc: {0} since: {1}", procName, lastUsedDay);
                    }
                }
            }
        }
    }

    public class DatabasesUsage
    {
        public DateTime GeneratedDate { get; set; }
        public SortedList<DateTime, long> DayGenerationExecutions { get; set; }
        public Dictionary<string, DatabaseUsage> Databases { get; set; }
        public DatabasesUsage()
        {
            Databases = new Dictionary<string, DatabaseUsage>();
            DayGenerationExecutions = new SortedList<DateTime, long>();
        }

    }

    public class DatabaseUsage
    {
        public Dictionary<string, TableUsage> Tables { get; set; }
        public Dictionary<string, ProcedureUsage> Procedures { get; set; }
        public DatabaseUsage()
        {
            Tables = new Dictionary<string, TableUsage>();
            Procedures = new Dictionary<string, ProcedureUsage>();
        }
    }

    public class TableUsage
    {
        public SortedList<DateTime, long> SeqScans { get; set; }
        public SortedList<DateTime, long> IdxScans { get; set; }
        public Dictionary<string, IndexUsage> Indices { get; set; }
        public TableUsage()
        {
            SeqScans = new SortedList<DateTime, long>();
            IdxScans = new SortedList<DateTime, long>();
            Indices = new Dictionary<string, IndexUsage>();
        }
    }

    public class IndexUsage
    {
        public SortedList<DateTime, long> IdxScans { get; set; }
        public IndexUsage()
        {
            IdxScans = new SortedList<DateTime, long>();
        }
    }

    public class ProcedureUsage
    {
        public SortedList<DateTime, long> Calls { get; set; }
        public ProcedureUsage()
        {
            Calls = new SortedList<DateTime, long>();
        }
    }
}
