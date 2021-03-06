using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace TVRename
{
    /// <summary>
    /// Handles the multithreaded nature of actioning many actions at the same time. It will provide a UI to update the user on the status of the execution if required.
    /// </summary>
    public class ActionEngine
    {
        
        private Thread actionProcessorThread;
        private bool actionPause;
        private List<Thread> actionWorkers;
        private Semaphore[] actionSemaphores;
        private bool actionStarting;

        private readonly TVRenameStats mStats; //reference to the main TVRenameStats, so we can udpate the counts

        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly NLog.Logger threadslogger = NLog.LogManager.GetLogger("threads");

        /// <summary>
        /// Asks for execution to pause
        /// </summary>
        public void Pause() { this.actionPause = true; }
        
        /// <summary>
        /// Asks for execution to resume
        /// </summary>
        public void Unpause() { this.actionPause = false; }

        public ActionEngine(TVRenameStats stats)
        {
            this.mStats = stats;

        }
        
        /// <summary>
        /// Processes an Action by running it.
        /// </summary>
        /// <param name="infoIn">A ProcessActionInfo to be processed. It will contain the Action to be processed</param>
        public void ProcessSingleAction(Object infoIn)
        {
            try
            {
                if (!(infoIn is ProcessActionInfo info))
                    return;

                this.actionSemaphores[info.SemaphoreNumber].WaitOne(); // don't start until we're allowed to
                this.actionStarting = false; // let our creator know we're started ok

                Action action = info.TheAction;
                if (action != null)
                {
                    logger.Trace("Triggering Action: {0} - {1} - {2}", action.Name, action.Produces, action.ToString());
                    action.Go(ref this.actionPause, this.mStats);
                }


                this.actionSemaphores[info.SemaphoreNumber].Release(1);
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Unhandled Exception in Process Single Action");
                return;
            }
        }



        private void WaitForAllActionThreadsAndTidyUp()
        {
            if (this.actionWorkers != null)
            {
                foreach (Thread t in this.actionWorkers)
                {
                    if (t.IsAlive)
                        t.Join();
                }
            }

            this.actionWorkers = null;
            this.actionSemaphores = null;
        }



        /// <summary>
        /// Processes a set of actions, running them in a multi-threaded way based on the application's settings.
        /// </summary>
        /// <param name="theList">An ItemList to be processed.</param>
        /// <param name="showUI">Whether or not we should display a UI to inform the user about progress.</param>
        public void DoActions(ItemList theList, bool showUI)
        {
            logger.Info("**********************");
            logger.Info("Doing Selected Actions....");
            if (theList == null)
                return;

            // Run tasks in parallel (as much as is sensible)

            ActionQueue[] queues = ActionProcessorMakeQueues(theList);
            this.actionPause = false;

            // If not /hide, show CopyMoveProgress dialog

            CopyMoveProgress cmp = null;
            if (showUI)
                cmp = new CopyMoveProgress(this, queues);

            this.actionProcessorThread = new Thread(ActionProcessor)
            {
                Name = "ActionProcessorThread"
            };

            this.actionProcessorThread.Start(queues);

            if ((cmp != null) && (cmp.ShowDialog() == DialogResult.Cancel))
                this.actionProcessorThread.Abort();

            this.actionProcessorThread.Join();

            theList.RemoveAll(x => (x is Action) && ((Action)x).Done && !((Action)x).Error);

            foreach (Item sli in theList)
            {
                if (sli is Action slia)
                {
                    logger.Warn("Failed to complete the following action: {0}, doing {1}. Error was {2}", slia.Name, slia.ToString(), slia.ErrorText);
                }
            }

            logger.Info("Completed Selected Actions");
            logger.Info("**************************");

        }

        private void ActionProcessor(object queuesIn)
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(queuesIn is ActionQueue[]);
#endif
            try
            {
                if (!(queuesIn is ActionQueue[] queues))
                    return;

                int n = queues.Length;

                this.actionWorkers = new List<Thread>();
                this.actionSemaphores = new Semaphore[n];

                for (int i = 0; i < n; i++)
                {
                    this.actionSemaphores[i] =
                        new Semaphore(queues[i].ParallelLimit,
                            queues[i].ParallelLimit); // allow up to numWorkers working at once
                    logger.Info("Setting up '{0}' worker, with {1} threads in position {2}.", queues[i].Name,
                        queues[i].ParallelLimit, i);
                }


                try
                {
                    for (; ; )
                    {
                        while (this.actionPause)
                            Thread.Sleep(100);

                        // look through the list of semaphores to see if there is one waiting for some work to do
                        bool allDone = true;
                        int which = -1;
                        for (int i = 0; i < n; i++)
                        {
                            // something to do in this queue, and semaphore is available
                            if (queues[i].ActionPosition < queues[i].Actions.Count)
                            {
                                allDone = false;
                                if (this.actionSemaphores[i].WaitOne(20, false))
                                {
                                    which = i;
                                    break;
                                }
                            }
                        }

                        if ((which == -1) && (allDone))
                            break; // all done!

                        if (which == -1)
                            continue; // no semaphores available yet, try again for one

                        ActionQueue q = queues[which];
                        Action act = q.Actions[q.ActionPosition++];

                        if (act == null)
                            continue;

                        if (!act.Done)
                        {
                            Thread t = new Thread(ProcessSingleAction)
                            {
                                Name = "ProcessSingleAction(" + act.Name + ":" + act.ProgressText + ")"
                            };
                            this.actionWorkers.Add(t);
                            this.actionStarting = true; // set to false in thread after it has the semaphore
                            t.Start(new ProcessActionInfo(which, act));

                            int nfr = this.actionSemaphores[which]
                                .Release(1); // release our hold on the semaphore, so that worker can grab it
                            threadslogger.Trace("ActionProcessor[" + which + "] pool has " + nfr + " free");
                        }

                        while (this.actionStarting) // wait for thread to get the semaphore
                            Thread.Sleep(10); // allow the other thread a chance to run and grab

                        // tidy up any finished workers
                        for (int i = this.actionWorkers.Count - 1; i >= 0; i--)
                        {
                            if (!this.actionWorkers[i].IsAlive)
                                this.actionWorkers.RemoveAt(i); // remove dead worker
                        }
                    }

                    WaitForAllActionThreadsAndTidyUp();
                }
                catch (ThreadAbortException)
                {
                    foreach (Thread t in this.actionWorkers)
                        t.Abort();
                    WaitForAllActionThreadsAndTidyUp();
                }
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Unhandled Exception in ActionProcessor");
                foreach (Thread t in this.actionWorkers)
                    t.Abort();
                WaitForAllActionThreadsAndTidyUp();
                return;
            }
        }


        private static ActionQueue[] ActionProcessorMakeQueues(ItemList theList)
        {
            // Take a single list
            // Return an array of "ActionQueue" items.
            // Each individual queue is processed sequentially, but all the queues run in parallel
            // The lists:
            //     - #0 all the cross filesystem moves, and all copies
            //     - #1 all quick "local" moves
            //     - #2 NFO Generator list
            //     - #3 Downloads (rss torrent, thumbnail, folder.jpg) across Settings.ParallelDownloads lists
            // We can discard any non-action items, as there is nothing to do for them

            ActionQueue[] queues = new ActionQueue[4];
            queues[0] = new ActionQueue("Move/Copy", 1); // cross-filesystem moves (slow ones)
            queues[1] = new ActionQueue("Move/Delete", 1); // local rename/moves
            queues[2] = new ActionQueue("Write Metadata", 4); // writing KODI NFO files, etc.
            queues[3] = new ActionQueue("Download", TVSettings.Instance.ParallelDownloads); // downloading torrents, banners, thumbnails

            foreach (Item sli in theList)
            {
                if (!(sli is Action action))
                    continue; // skip non-actions

                if ((action is ActionWriteMetadata) || (action is ActionDateTouch)) // base interface that all metadata actions are derived from
                    queues[2].Actions.Add(action);
                else if ((action is ActionDownloadImage) || (action is ActionRSS))
                    queues[3].Actions.Add(action);
                else if (action is ActionCopyMoveRename)
                    queues[(action as ActionCopyMoveRename).QuickOperation() ? 1 : 0].Actions.Add(action);
                else if ((action is ActionDeleteFile) || (action is ActionDeleteDirectory))
                    queues[1].Actions.Add(action);
                else
                {
#if DEBUG
                    System.Diagnostics.Debug.Fail("Unknown action type for making processing queue");
#endif
                    logger.Error("No action type found for {0}, Please follow up with a developer.", action.GetType());
                    queues[3].Actions.Add(action); // put it in this queue by default
                }
            }
            return queues;
        }


        #region Nested type: ProcessActionInfo

        private class ProcessActionInfo
        {
            public readonly int SemaphoreNumber;
            public readonly Action TheAction;

            public ProcessActionInfo(int n, Action a)
            {
                this.SemaphoreNumber = n;
                this.TheAction = a;
            }
        };

        #endregion

    }
}
