using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    public class TestItems
    {
        public List<Func<TestStatus, TestResult>> testFuncList { get; private set; }

        public TestItems()
        {
            if (testFuncList == null)
                testFuncList = new List<Func<TestStatus, TestResult>>();
        }

        public void AddTasks(TestTask task)
        {
            if (testFuncList.Count > 0) testFuncList.Clear();

            testFuncList.Add(task.Init);
            testFuncList.Add(task.TestWLCurrent);
            testFuncList.Add(task.TestBTCurrent);
            testFuncList.Add(task.WriteMac);
            testFuncList.Add(task.WriteFakeMap);
            testFuncList.Add(task.WriteEfuse);
            testFuncList.Add(task.CheckEfuse);
            // power off module must be in end test
            testFuncList.Add(task.DeInit);
        }
    }
}
