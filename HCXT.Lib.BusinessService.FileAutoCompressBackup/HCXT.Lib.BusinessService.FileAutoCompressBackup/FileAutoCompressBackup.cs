using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using HCXT.Lib.Interface.StandardService;

namespace HCXT.Lib.BusinessService.FileAutoCompressBackup
{
    /// <summary>
    /// 
    /// </summary>
    public class FileAutoCompressBackup : IBusinessService
    {
        #region 实现接口IBusinessService
        private string _serviceName;
        private string _description;
        private int _timeOut;
        private int _timeSpan;
        private bool _enabled;
        private bool _isRunning;
        private string _serviceConfigString;
        private object _otherArgs;

        /// <summary>启动服务</summary>
        public void Start()
        {
            const string logHead = "[FileAutoCompressBackup.Start] ";
            Cow("Info", string.Format("{0}服务[{1}]主线程已启动。", logHead, _serviceName));
            try
            {
                foreach (var service in _services)
                {
                    service.OnCow += service_OnCow;
                    service.Start();
                    Cow("Info", string.Format("{0}服务策略[{1}]子线程已启动。", logHead, service.Name));
                    Thread.Sleep(100);
                }
                Cow("Info", string.Format("{0}服务[{1}]全部策略子线程已启动完毕。", logHead, _serviceName));
            }
            catch (Exception err)
            {
                Cow("Error", string.Format("{0}服务[{1}]启动策略子线程时发生严重异常！异常信息：{2}\r\n堆栈：{3}", logHead, _serviceName, err.Message, err.StackTrace));
            }
        }
        /// <summary>停止服务</summary>
        public void Stop()
        {
            const string logHead = "[FileAutoCompressBackup.Stop] ";
            try
            {
                foreach (var service in _services)
                {
                    service.Stop();
                    Cow("Info", string.Format("{0}服务策略[{1}]子线程已停止。", logHead, service.Name));
                    Thread.Sleep(100);
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
            Cow("Info", string.Format("{0}服务[{1}]主线程已停止。", logHead, _serviceName));
        }
        /// <summary>服务名称</summary>
        public string ServiceName
        {
            get { return _serviceName; }
            set { _serviceName = value; }
        }
        /// <summary>服务中文名称</summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
        /// <summary>业务处理超时时长（毫秒）</summary>
        public int TimeOut
        {
            get { return _timeOut; }
            set { _timeOut = value; }
        }
        /// <summary>扫描步长时间（毫秒）</summary>
        public int TimeSpan
        {
            get { return _timeSpan; }
            set { _timeSpan = value; }
        }
        /// <summary>服务是否允许启用</summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }
        /// <summary>服务是否在运行中</summary>
        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; }
        }
        /// <summary>服务配置XML串</summary>
        public string ServiceConfigString
        {
            get { return _serviceConfigString; }
            set
            {
                const string logHead = "[FileAutoCompressBackup.ServiceConfigString.set] ";
                #region 传入的服务配置XML串格式示例
                /*
<HCXT.Lib.BusinessService.FileAutoCompressBackup>
  <ProgramName>定时自动压缩备份工具</ProgramName>
  <Service>
    <Name>BackupTicketSourceMessage</Name>
    <ServiceDesc>压缩原始原子话单消息数据导入文件</ServiceDesc>
    <ModelBatFile>BackupLogs.bat</ModelBatFile>
    <BackupBy>Hour</BackupBy>
    <PerUnits>1</PerUnits>
    <SourceFolder>E:\HCXT\RealTimeAnalysis_SourceMessage\T_Meter_Analysis_SourceMessage\Succ\</SourceFolder>
    <ObjectFolder>E:\HCXT\RealTimeAnalysis_SourceMessage\T_Meter_Analysis_SourceMessage\Succ\</ObjectFolder>
    <SourceFileNameMask>T_Ticket_SrcMsg_%BackupBy%*.xml</SourceFileNameMask>
    <ObjectFileNameMask>T_Ticket_SrcMsg_%BackupBy%.rar</ObjectFileNameMask>
  </Service>
</HCXT.Lib.BusinessService.FileAutoCompressBackup>
                 */
                #endregion
                try
                {
                    var dom = new XmlDocument();
                    dom.LoadXml(value);
                    const string nodeStr = "//HCXT.Lib.BusinessService.FileAutoCompressBackup/Service";
                    var xnl = dom.SelectNodes(nodeStr);
                    if (xnl == null)
                    {
                        Cow("Fatal", string.Format("{0}致命的异常。加载配置节点[{1}]失败！传入的XML[{2}]", logHead, nodeStr, value));
                        return;
                    }
                    for (var i = 0; i < xnl.Count; i++)
                    {
                        var xn = xnl[i];
                        var service = new FACB
                        {
                            // ReSharper disable PossibleNullReferenceException
                            Name = xn.SelectSingleNode("Name").InnerText,
                            ServiceDesc = xn.SelectSingleNode("ServiceDesc").InnerText,
                            ModelBatFile = xn.SelectSingleNode("ModelBatFile").InnerText,
                            BackupBy = xn.SelectSingleNode("BackupBy").InnerText,
                            PerUnits = xn.SelectSingleNode("PerUnits").InnerText,
                            SourceFolder = xn.SelectSingleNode("SourceFolder").InnerText,
                            ObjectFolder = xn.SelectSingleNode("ObjectFolder").InnerText,
                            SourceFileNameMask = xn.SelectSingleNode("SourceFileNameMask").InnerText,
                            ObjectFileNameMask = xn.SelectSingleNode("ObjectFileNameMask").InnerText
                            // ReSharper restore PossibleNullReferenceException
                        };
                        _services.Add(service);
                    }
                    Thread.Sleep(100);
                }
                catch (Exception err)
                {
                    Cow("Fatal", string.Format("{0}发生致命的异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                }
                _serviceConfigString = value;
            }
        }

        void service_OnCow(params object[] args)
        {
            Cow(args[1].ToString(), args[2].ToString());
        }
        /// <summary>其他参数</summary>
        public object OtherArgs
        {
            get { return _otherArgs; }
            set { _otherArgs = value; }
        }
        /// <summary>业务回调事件</summary>
        public event DelegateBusinessInvoke BusinessInvoke;
        #endregion


        private readonly List<FACB> _services = new List<FACB>();


        private void Log(string logType, string logMsg)
        {
            if (BusinessInvoke != null)
                BusinessInvoke("log", logType, logMsg);
        }
        private void Cow(string logType, string message)
        {
            Log(logType, message);
            Console.Out.WriteLine("{0} [{1}] {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), logType, message);
        }
    }
}
