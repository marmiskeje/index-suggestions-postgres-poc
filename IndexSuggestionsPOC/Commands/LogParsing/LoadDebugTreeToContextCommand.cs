using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class LoadDebugTreeToContextCommand : ChainableCommand
    {
        private readonly Func<string> getInputFunc;
        private readonly Action<JObject> setOutputAction;
        public LoadDebugTreeToContextCommand(Func<string> getInputFunc, Action<JObject> setOutputAction)
        {
            this.getInputFunc = getInputFunc;
            this.setOutputAction = setOutputAction;
        }

        //for now string
        private string ConvertToJsonProperty(string input)
        {
            string result = input.Trim();
            if (!String.IsNullOrEmpty(result) && !input.StartsWith("\""))
            {
                result = "\"" + input + "\"";
            }
            return result;
        }
        protected override void OnExecute()
        {
            Regex constantRegex = new Regex(@":constvalue [^<>].*?\[[\s|\S]*?\]");
            Regex planNameRegex = new Regex(@":plan_name .*? :");
            Regex attributesWithoutValues = new Regex(@":[a-z|A-Z]*? :");
            Dictionary<string, string> replacedConstants = new Dictionary<string, string>();
            Dictionary<string, string> replacedPlanNames = new Dictionary<string, string>();
            string inputParseTreeStr = getInputFunc();
            string parseTreeStr = inputParseTreeStr.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ").Trim();
            while (parseTreeStr.Contains("  "))
            {
                parseTreeStr = parseTreeStr.Replace("  ", " ");
            }
            bool doNextAttributesWithoutValuesIteration = true;
            while (doNextAttributesWithoutValuesIteration)
            {
                doNextAttributesWithoutValuesIteration = false;
                foreach (Match m in attributesWithoutValues.Matches(parseTreeStr))
                {
                    string toReplace = m.Value.Substring(0, m.Value.Length - 1).Trim() + " <> :";
                    parseTreeStr = parseTreeStr.Replace(m.Value, toReplace);
                    doNextAttributesWithoutValuesIteration = true; // two attributes without values might be next to each other, we need another iteration
                }
            }
            foreach (Match m in planNameRegex.Matches(parseTreeStr))
            {
                string identifier = Guid.NewGuid().ToString().Replace("-", "");
                string planName = m.Value.Substring(11).Trim(); // without :plan_name
                planName = planName.Substring(0, planName.Length - 1).Trim(); // without ending :
                parseTreeStr = parseTreeStr.Replace(planName, identifier);
                replacedPlanNames.Add(identifier, planName.Replace("\t", "").Replace(@"\", ""));
            }
            foreach (Match m in constantRegex.Matches(parseTreeStr))
            {
                string constantIdentifier = Guid.NewGuid().ToString().Replace("-", "");
                string constantValue = m.Value.Substring(12).Trim(); //without ":constvalue "
                parseTreeStr = parseTreeStr.Replace(constantValue, constantIdentifier);
                replacedConstants.Add(constantIdentifier, constantValue.Replace("\t", ""));
            }
            var inputEntries = parseTreeStr.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < inputEntries.Length; i++)
            {
                string input = inputEntries[i].Trim();
                if (input.StartsWith("({"))
                {
                    input = "[ { " + ConvertToJsonProperty(input.Substring(2)) + ": {";
                }
                else if (input.StartsWith("("))
                {
                    string originalInput = input;
                    int length = input.Length - 1;
                    if (originalInput.EndsWith(")"))
                    {
                        length = input.Length - 2;
                    }
                    input = ConvertToJsonProperty(input.Substring(1, length));
                    input = "[ " + input;
                    if (originalInput.EndsWith(")"))
                    {
                        input = input + " ], ";
                    }
                    else
                    {
                        input = input + ", ";
                    }
                }
                else if (input.StartsWith("{"))
                {
                    input = "{ " + ConvertToJsonProperty(input.Substring(1)) + ": {";
                }
                else if (input.StartsWith(":"))
                {
                    input = ConvertToJsonProperty(input.Substring(1)) + ": ";
                }
                else if(input.EndsWith("}") || input.EndsWith(")"))
                {
                    string originalInput = input;
                    string val = input.Replace("}", "").Replace(")", "");
                    input = input.Replace(val, ConvertToJsonProperty(val));
                    input = input.Replace(")", "]").Replace("}", "} }") + ", ";
                }
                else
                {
                    input = ConvertToJsonProperty(input);
                    input = input + ",";
                }
                inputEntries[i] = input;
            }
            string json = String.Join(" ", inputEntries);
            json = json.Substring(0, json.Length - 2); // remove ending ", "
            foreach (var item in replacedConstants)
            {
                json = json.Replace(item.Key, item.Value);
            }
            foreach (var item in replacedPlanNames)
            {
                json = json.Replace(item.Key, item.Value);
            }
            var result = JObject.Parse(json);
#if DEBUG
            if (Debugger.IsAttached)
            {
                var niceJson = result.ToString(Formatting.Indented); 
            }
#endif
            setOutputAction(result);
        }
    }
}
