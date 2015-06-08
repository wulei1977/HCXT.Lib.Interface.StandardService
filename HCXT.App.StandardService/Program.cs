using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using HCXT.Lib.Interface.StandardService;

namespace HCXT.App.StandardService
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            //程序开始日志
            string logBeginStr =
                string.Format("\r\n{0}\r\n-  开始应用程序{1}-\r\n-  版本号：V{2}{3}-\r\n{0}", new string('-', 110),
                              new string(' ', 94), Config.Version, new string(' ', 97 - Config.Version.Length));
            Config.Log("INFO", logBeginStr);
            // 控制台输出 开始运行信息
            Config.Cow(string.Format("[{0}][Ver {1}]开始运行。", Config.ProgramName, Config.Version));

            // 设置控制台窗体标题
            Console.Title = string.Format("{0} Ver {1}", Config.ProgramName, Config.Version);

            Config.Cow("Info", string.Format("当前进程PID：{0}", Config.PId));

            var serviceList = new List<IBusinessService>();

            for (var i = 0; i < Config.Services.Length; i++)
            {
                string sName = Config.Services[i];
                Config.CowLog("Info", string.Format("准备加载服务[{0}]的配置信息。", sName));
                var node = Config.DomConfig.SelectSingleNode(string.Format("//root/{0}", sName));
                if (node == null)
                {
                    Config.CowLog("Fatal", string.Format("配置文件中未找到服务[{0}]的配置节点。该服务无法启动。", sName));
                    continue;
                }
                var arr = sName.Split('.');
                var ass = Assembly.LoadFile(string.Format("{0}\\{1}.dll", Config.AppPath, sName));
                var service = (IBusinessService)ass.CreateInstance(sName + "." + arr[arr.Length - 1]);
                service.BusinessInvoke += service_BusinessInvoke;
                service.ServiceConfigString = node.OuterXml;
                service.Start();
                serviceList.Add(service);
            }


            // 控制台命令检测
            while (true)
            {
                var sCmd = Console.ReadLine();
                if (!string.IsNullOrEmpty(sCmd) && sCmd.Trim().ToLower() == "exit")
                    break;
                Console.WriteLine("无法识别的命令。");
            }

            for (var i = 0; i < serviceList.Count; i++)
            {
                serviceList[i].Stop();
            }

        }

        static void service_BusinessInvoke(params object[] args)
        {
            var argType = args[0].ToString();
            switch (argType)
            {
                case "log":
                    Config.Log(args[1].ToString(), args[2].ToString());
                    break;
            }
        }
    }
}
