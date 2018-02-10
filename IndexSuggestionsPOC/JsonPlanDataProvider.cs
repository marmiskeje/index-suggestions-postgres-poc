using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class JsonPlanDataProvider
    {
        private readonly JToken jObject;
        private readonly Lazy<ExecutionPlanRoot> root;

        public ExecutionPlanRoot RootPlan
        {
            get { return root.Value; }
        }

        public JsonPlanDataProvider(JToken jObject)
        {
            this.jObject = jObject;
            this.root = new Lazy<ExecutionPlanRoot>(InitializeRoot);
        }

        private ExecutionPlanRoot InitializeRoot()
        {
            ExecutionPlanRoot root = new ExecutionPlanRoot();
            var rootPlan = jObject.SelectToken("..Plan");
            root.StartupCost = rootPlan.SelectToken("['Startup Cost']").Value<decimal>();
            root.TotalCost = rootPlan.SelectToken("['Total Cost']").Value<decimal>();
            var indexToken = rootPlan.SelectToken("['Index Name']");
            if (indexToken != null)
            {
                root.IndexName = indexToken.Value<String>();
                if (root.IndexName.StartsWith("\"")) // index name is in "
                {
                    root.IndexName = root.IndexName.Substring(1);
                    root.IndexName = root.IndexName.Substring(0, root.IndexName.Length - 1);
                    root.IndexName = root.IndexName.Trim();
                }
            }
            foreach (var plans in jObject.SelectTokens("..Plans"))
            {
                foreach (var plan in plans.Children())
                {
                    ExecutionPlanNode node = new ExecutionPlanNode();
                    node.StartupCost = plan.SelectToken("['Startup Cost']").Value<decimal>();
                    node.TotalCost = plan.SelectToken("['Total Cost']").Value<decimal>();
                    var indexToken2 = plan.SelectToken("['Index Name']");
                    if (indexToken2 != null)
                    {
                        node.IndexName = indexToken2.Value<String>();
                        if (node.IndexName.StartsWith("\"")) // index name is in "
                        {
                            node.IndexName = node.IndexName.Substring(1);
                            node.IndexName = node.IndexName.Substring(0, node.IndexName.Length - 1);
                            node.IndexName = node.IndexName.Trim();
                        }
                    }
                    root.Plans.Add(node);
                }
            }
            return root;
        }
    }

}
