﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Gunter.Data;
using Reusable.Extensions;
using Reusable.Utilities.Mailr.Models;

namespace Gunter.Reporting.Modules.Tabular
{
    public class QueryInfo : Module, ITabular
    {
        public TableOrientation Orientation => TableOrientation.Vertical;

        public bool HasFoot => false;

        [DefaultValue("Timestamp")]
        public string TimestampColumn { get; set; }

        // Custom TimeSpan Format Strings https://msdn.microsoft.com/en-us/library/ee372287(v=vs.110).aspx

        [DefaultValue(@"mm\:ss\.fff")]
        public string TimespanFormat { get; set; }

        public override IModuleDto CreateDto(TestContext context)
        {
            // Initialize the data-table;
            var section = new ModuleDto<QueryInfo>
            {
                Heading = Heading.Format(context.RuntimeProperties),
                Data = new HtmlTable(HtmlTableColumn.Create
                (
                    ("Property", typeof(string)),
                    ("Value", typeof(string))
                ))
            };
            var table = section.Data;

            table.Body.Add("Type", context.Query.GetType().Name);
            table.Body.NewRow().Update(Columns.Property, "Command").Update(Columns.Value, context.Query, "query");
            table.Body.Add("Results", context.Data.Rows.Count.ToString());
            table.Body.Add("Elapsed", $"{RuntimeProperty.BuiltIn.TestCounter.GetDataElapsed.ToFormatString(TimespanFormat)}".Format(context.RuntimeProperties));

            var hasTimestampColumn = context.Data.Columns.Contains(TimestampColumn);
            var hasRows = context.Data.Rows.Count > 0; // If there are no rows Min/Max will throw.

            if (hasTimestampColumn && hasRows)
            {
                var timestampMin = context.Data.AsEnumerable().Min(r => r.Field<DateTime>(TimestampColumn));
                var timestampMax = context.Data.AsEnumerable().Max(r => r.Field<DateTime>(TimestampColumn));

                table.Body.Add(new List<string> { "Timestamp: min", timestampMin.ToString(CultureInfo.InvariantCulture) });
                table.Body.Add(new List<string> { "Timestamp: max", timestampMax.ToString(CultureInfo.InvariantCulture) });

                var timespan = timestampMax - timestampMin;
                table.Body.Add(new List<string> { "Timespan", timespan.ToString(TimespanFormat, CultureInfo.InvariantCulture) });
            }

            return section;
        }

        private static class Columns
        {
            public const string Property = nameof(Property);

            public const string Value = nameof(Value);
        }
    }
}