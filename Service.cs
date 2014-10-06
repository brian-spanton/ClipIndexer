using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using foobalator;

namespace ClipIndexer
{
    // BrianSp: This class manages the service when run in that mode.
    public class Service : ServiceBase
    {
        private static Service s_Service;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            SystemState.Settings = new ConfigFileSettings();

            try
            {
                // BrianSp: This app can be run as a command line utility if you pass any argument to it.  Otherwise it assumes
                // it is executing in the context of an installed windows service.

                if (args.Length == 0)
                {
                    ServiceBase.Run(new ServiceBase[] { new Service() });
                }
                else if (args.Length == 1)
                {
                    if (args[0] == "/console" || args[0] == "/c")
                        Worker.Run();
                    if (args[0] == "/dir" || args[0] == "/d")
                        Worker.Index(Directory.GetCurrentDirectory());
                    else
                        Help();
                }
                else if (args.Length == 2)
                {
                    if (args[0] == "/dir" || args[0] == "/d")
                        Worker.Index(args[1]);
                    else
                        Help();
                }
                else
                {
                    Help();
                }
            }
            catch (Exception e)
            {
                Log.Write(e);
                throw;
            }
        }

        private static void Help()
        {
            Log.ShowLine(Worker.Name + " usage:");
            Log.ShowLine("    /console or /c - Run service as a console app");
            Log.ShowLine("    /dir or /d [targetdir] - Run on current dir");
            Log.ShowLine("    (anything else) - Display this help text");
        }

        public Service()
        {
            ServiceName = Worker.Name;
        }

        protected override void OnStart(string[] args)
        {
            s_Service = this;

            // BrianSp: Kick off a new thread because the service manager owns this one.  This thread
            // runs the main application entry point.
            Thread thread = new Thread(new ThreadStart(StaticServiceRun));
            thread.Start();
        }

        private static void StaticServiceRun()
        {
            s_Service.ServiceRun();
        }

        private void ServiceRun()
        {
            try
            {
                Worker.Run();
            }
            catch
            {
                Stop();
            }
        }

        protected override void OnStop()
        {
            // BrianSp: If the service control manager sends a stop message, set the event to let the app thread
            // know it's time to shutdown.
            Worker.End();
        }
    }
}
