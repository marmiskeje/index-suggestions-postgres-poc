using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    class ApplyWorkloadDefinitionCommand : ChainableCommand
    {
        private readonly LogParsingContext context;
        public ApplyWorkloadDefinitionCommand(LogParsingContext context)
        {
            this.context = context;
        }
        protected override void OnExecute()
        {
            bool canContinue = true;
            if (context.Workload != null)
            {
                var workload = context.Workload;
                if (workload.Applications != null)
                {
                    canContinue = canContinue && ApplyWorkloadProperty(context.LogEntry.ApplicationName, context.Workload.Applications);
                }
                if (!String.IsNullOrEmpty(workload.DatabaseName))
                {
                    canContinue = canContinue && context.LogEntry.DatabaseName == workload.DatabaseName;
                }
                if (workload.DateTimeSlots != null)
                {
                    bool initTmpContinue = workload.DateTimeSlots.RestrictionType == WorkloadPropertyRestrictionType.Allowed ? false : true;

                    bool tmpCanContinue = initTmpContinue;
                    var date = context.LogEntry.Timestamp;
                    foreach (var s in workload.DateTimeSlots.Values)
                    {
                        if (date.DayOfWeek == s.DayOfWeek && date.TimeOfDay >= s.StartTime && date.TimeOfDay <= s.EndTime)
                        {
                            tmpCanContinue = !initTmpContinue;
                            break;
                        }
                    }
                    canContinue = canContinue && tmpCanContinue;
                }
                if (workload.QueryThresholds != null)
                {
                    // we can´t map duration to query
                    //canContinue = canContinue && context.LogEntry.Duration >= workload.QueryThresholds.MinDuration;
                }
                if (workload.Users != null)
                {
                    canContinue = canContinue && ApplyWorkloadProperty(context.LogEntry.UserName, context.Workload.Users);
                }
            }
            IsEnabledSuccessorCall = canContinue;
        }

        private bool ApplyWorkloadProperty<T>(T logEntryValue, IWorkloadPropertyValuesDefinition<T> workloadPropertyDefinition)
        {
            if (workloadPropertyDefinition.RestrictionType == WorkloadPropertyRestrictionType.Allowed)
            {
                return workloadPropertyDefinition.Values.Contains(logEntryValue);
            }
            else if (workloadPropertyDefinition.RestrictionType == WorkloadPropertyRestrictionType.Disallowed)
            {
                return !workloadPropertyDefinition.Values.Contains(logEntryValue);
            }
            return true;
        }
    }
}
