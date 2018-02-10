using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class PlanTreeDataProvider
    {
        private readonly JObject jObject;
        private readonly Lazy<ExecutionPlanRoot> root;

        public ExecutionPlanRoot RootPlan
        {
            get { return root.Value; }
        }

        public PlanTreeDataProvider(JObject jObject)
        {
            this.jObject = jObject;
            this.root = new Lazy<ExecutionPlanRoot>(InitializeRoot);
        }

        private ExecutionPlanRoot InitializeRoot()
        {
            ExecutionPlanRoot root = new ExecutionPlanRoot();
            foreach (var plan in jObject.SelectTokens("..planTree"))
            {
                root.StartupCost = plan.First.First.SelectToken("startup_cost").Value<decimal>();
                root.TotalCost = plan.First.First.SelectToken("total_cost").Value<decimal>();
            }
            foreach (var plan in jObject.SelectTokens("..lefttree"))
            {
                if (plan.ToString() != "<>")
                {
                    var node = new ExecutionPlanNode();
                    node.StartupCost = plan.First.First.SelectToken("startup_cost").Value<decimal>();
                    node.TotalCost = plan.First.First.SelectToken("total_cost").Value<decimal>();
                    root.Plans.Add(node); 
                }
            }
            foreach (var plan in jObject.SelectTokens("..rightrree"))
            {
                if (plan.ToString() != "<>")
                {
                    var node = new ExecutionPlanNode();
                    node.StartupCost = plan.First.First.SelectToken("startup_cost").Value<decimal>();
                    node.TotalCost = plan.First.First.SelectToken("total_cost").Value<decimal>();
                    root.Plans.Add(node);
                }
            }
            return root;
        }
    }

    class ExecutionPlanRoot
    {
        public decimal StartupCost { get; set; }
        public decimal TotalCost { get; set; }
        public String IndexName { get; set; }

        public List<ExecutionPlanNode> Plans { get; private set; }

        public ExecutionPlanRoot()
        {
            Plans = new List<ExecutionPlanNode>();
        }
    }


    class ExecutionPlanNode
    {
        public decimal StartupCost { get; set; }
        public decimal TotalCost { get; set; }
        public String IndexName { get; set; }
    }
}
