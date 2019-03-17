using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SEMI.Schdule
{
    public class Handler
    {
        private static Handler instance = new Handler();

        private static List<Model.Interface.ISchduleTask> taskQueue = new List<Model.Interface.ISchduleTask>();
        private Handler() { }
        public static Handler GetInstance() { return instance; }

        private bool processStatus = false;

        /// <summary>
        /// 将需要定时执行的任务添加到静态任务队列
        /// </summary>
        /// <param name="task"></param>
        public void SendTask(Model.Interface.ISchduleTask task)
        {
            if (taskQueue == null || task == null) return;
            lock (taskQueue) { taskQueue.Add(task); }
        }

        /// <summary>
        /// 处理任务队列中的任务,定时执行此方法
        /// </summary>
        public void ProcessTaskQueue()
        {
            EventLog.WriteEntry(Process.GetCurrentProcess().ProcessName, string.Format("processStatus = {0}", processStatus));
            if (processStatus || taskQueue == null || taskQueue.Count == 0) return;
            try
            {
                processStatus = true;
                lock (taskQueue)
                {
                    string started = DateTime.Now.ToString("HH:mm:ss");
                    Task[] taskArray = new Task[taskQueue.Count];
                    for (int i = 0; i < taskQueue.Count; i++)
                    {
                        taskArray[i] = new Task((c) => { taskQueue[(int)c].Run(); }, i); //异步执行,必须将i作为参数传入Action
                        taskArray[i].Start();
                    }
                    Task.WaitAll(taskArray);

                    EventLog.WriteEntry(Process.GetCurrentProcess().ProcessName, string.Format("{0} - {1}\n{2}", started, DateTime.Now.ToString("HH:mm:ss"),
                        string.Join(",",taskQueue.Select(q=>q.ToString()).ToArray())));
                }
            }
            catch { throw; }
            finally { processStatus = false; }
        }
    }
}
