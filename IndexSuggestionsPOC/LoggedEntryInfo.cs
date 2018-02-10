using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class LoggedEntryInfo
    {
        private readonly Lazy<QueryTreeDataProvider> queryTreeDataProvider;
        private readonly Lazy<PlanTreeDataProvider> planTreeDataProvider;
        public DateTime Timestamp { get; set; }
        public string ProcessID { get; set; }
        public string ApplicationName { get; set; }
        public string UserName { get; set; }
        public string DatabaseName { get; set; }
        public string RemoteHostAndPort { get; set; }
        public string SessionID { get; set; }
        public long SessionLineNumber { get; set; }
        public LoggedVirtualTransactionIdentifier VirtualTransactionIdentifier { get; set; }
        public string TransactionID { get; set; }
        public TimeSpan Duration { get; set; }
        public string Statement { get; set; }
        public JObject QueryTree { get; set; }
        public JObject PlanTree { get; set; }

        public QueryTreeDataProvider QueryTreeDataProvider
        {
            get { return queryTreeDataProvider.Value; }
        }
        public PlanTreeDataProvider PlanTreeDataProvider
        {
            get { return planTreeDataProvider.Value; }
        }

        public LoggedEntryInfo()
        {
            queryTreeDataProvider = new Lazy<QueryTreeDataProvider>(() => new QueryTreeDataProvider(QueryTree));
            planTreeDataProvider = new Lazy<PlanTreeDataProvider>(() => new PlanTreeDataProvider(PlanTree));
        }
    }
    [DebuggerDisplay("{BackendID}/{LocalXID}")]
    class LoggedVirtualTransactionIdentifier
    {
        public string BackendID { get; set; }
        public string LocalXID { get; set; }
        public LoggedVirtualTransactionIdentifier(string str)
        {
            var split = str.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length >= 2)
            {
                BackendID = split[0];
                LocalXID = split[1];
            }
        }
    }
}
