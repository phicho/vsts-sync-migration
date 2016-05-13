﻿using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace TfsWitMigrator.Core
{
    public class WorkItemUpdate : ProcessingContextBase
    {
        string _queryBit;
        MigrationEngine _me;

        public WorkItemUpdate(MigrationEngine me, string queryBit) : base(me)
        {
            _me = me;
            _queryBit = queryBit;
        }

        public override string Name
        {
            get
            {
                return "WorkItemUpdate";
            }
        }

        internal override void InternalExecute()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            //////////////////////////////////////////////////
            WorkItemStoreContext targetStore = new WorkItemStoreContext(me.Target, WorkItemStoreFlags.BypassRules);

            TfsQueryContext tfsqc = new TfsQueryContext(targetStore);
            tfsqc.AddParameter("TeamProject", me.Target.Name);
            tfsqc.Query = string.Format(@"SELECT [System.Id], [System.Tags] FROM WorkItems WHERE  [System.WorkItemType] = 'Task'  AND  [System.State] = 'To Do'", _queryBit);
            WorkItemCollection  workitems = tfsqc.Execute();
            Trace.WriteLine(string.Format("Update {0} work items?", workitems.Count));
            //////////////////////////////////////////////////
            int current = workitems.Count;
            int count = 0;
            long elapsedms = 0;
            foreach (WorkItem workitem in workitems)
            {
                Stopwatch witstopwatch = new Stopwatch();
                witstopwatch.Start();
               
                workitem.Open();

                _me.ApplyFieldMappings(workitem);

                if (workitem.IsDirty)
                {
                    workitem.Save();
                }
                



                witstopwatch.Stop();
                elapsedms = elapsedms + witstopwatch.ElapsedMilliseconds;
                current--;
                count++;
                TimeSpan average = new TimeSpan(0, 0, 0, 0, (int)(elapsedms / count));
                TimeSpan remaining = new TimeSpan(0, 0, 0, 0, (int)(average.TotalMilliseconds * current));
                Trace.WriteLine(string.Format("Average time of {0} per work item and {1} estimated to completion", string.Format(@"{0:s\:fff} seconds", average), string.Format(@"{0:%h} hours {0:%m} minutes {0:s\:fff} seconds", remaining)));
            }
            //////////////////////////////////////////////////
            stopwatch.Stop();
            Console.WriteLine(@"DONE in {0:%h} hours {0:%m} minutes {0:s\:fff} seconds", stopwatch.Elapsed);
        }

    }
}