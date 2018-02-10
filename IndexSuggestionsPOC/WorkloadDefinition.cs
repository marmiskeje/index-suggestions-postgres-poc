using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexSuggestionsPOC
{
    public class WorkloadDefinition
    {
        public string DatabaseName { get; set; }
        public IWorkloadPropertyValuesDefinition<String> Users { get; set; }
        public IWorkloadPropertyValuesDefinition<WorkloadDateTimeSlot> DateTimeSlots { get; set; }
        public IWorkloadPropertyValuesDefinition<String> Tables { get; set; }
        public WorkloadQueryThresholds QueryThresholds { get; set; }
        public IWorkloadPropertyValuesDefinition<String> Applications { get; set; }
    }

    public class WorkloadQueryThresholds
    {
        public TimeSpan MinDuration { get; set; }
    }

    public class WorkloadDateTimeSlot
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
    }

    public class WorkloadPropertyValuesDefinition<T> : IWorkloadPropertyValuesDefinition<T>
    {
        public ISet<T> Values { get; set; }

        public WorkloadPropertyRestrictionType RestrictionType { get; set; }

        public WorkloadPropertyValuesDefinition()
        {
            Values = new HashSet<T>();
        }
    }

    public interface IWorkloadPropertyDefinition
    {
        WorkloadPropertyRestrictionType RestrictionType { get; }
    }

    public interface IWorkloadPropertyValuesDefinition<T> : IWorkloadPropertyDefinition
    {
        ISet<T> Values { get; }
    }

    public enum WorkloadPropertyRestrictionType
    {
        Allowed = 0,
        Disallowed = 1
    }
}
