using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using HCXT.Lib.Log;

namespace HCXT.App.StandardService
{
    /// <summary>
    /// 基础配置类
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// 静态构造方法
        /// </summary>
        static Config()
        {
            try
            {
                // 日志保存器对象
                _logger = new LogSaver(Log4NetConfigFile, Log4NetLaggerName);
                _loggerDebug = new LogSaver(Log4NetConfigFile, Log4NetLaggerNameDebug);
                _loggerWarn = new LogSaver(Log4NetConfigFile, Log4NetLaggerNameWarn);
                _loggerError = new LogSaver(Log4NetConfigFile, Log4NetLaggerNameError);
                _loggerFatal = new LogSaver(Log4NetConfigFile, Log4NetLaggerNameFatal);

                // 服务名称数组
                var s = GetStringlValueFromDom(PathServices, "").Trim(' ', '\r', '\n', '\t');
                _services = s.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            }
            catch(Exception err)
            {
                Cow("Fatal", string.Format("[Config.Config] 致命的异常！初始化配置信息时发生异常。异常信息：{0}\r\n堆栈：{1}", err.Message, err.StackTrace));
            }
        }

        #region 日志相关
        #region 常量
        /// <summary>log4netConfig文件路径</summary>
        private const string Log4NetConfigFile = "HCXT.App.StandardService.log4net";
        /// <summary>log4netLogger日志保存器名称(Info)</summary>
        private const string Log4NetLaggerName = "HCXT.App.StandardService.Info";
        /// <summary>log4netLogger日志保存器名称(Debug)</summary>
        private const string Log4NetLaggerNameDebug = "HCXT.App.StandardService.Debug";
        /// <summary>log4netLogger日志保存器名称(Warn)</summary>
        private const string Log4NetLaggerNameWarn = "HCXT.App.StandardService.Warn";
        /// <summary>log4netLogger日志保存器名称(Error)</summary>
        private const string Log4NetLaggerNameError = "HCXT.App.StandardService.Error";
        /// <summary>log4netLogger日志保存器名称(Fatal)</summary>
        private const string Log4NetLaggerNameFatal = "HCXT.App.StandardService.Fatal";
        /// <summary>LogSn文件路径</summary>
        private const string LogSnConfigFile = "LogSN.txt";
        #endregion

        #region 成员及属性
// ReSharper disable InconsistentNaming
        /// <summary>日志保存器对象</summary>
        private static readonly LogSaver _logger;
        /// <summary>日志保存器对象</summary>
        public static LogSaver Logger
        {
            get { return _logger; }
        }
        /// <summary>日志保存器对象(Debug)</summary>
        private static readonly LogSaver _loggerDebug;
        /// <summary>日志保存器对象(Debug)</summary>
        public static LogSaver LoggerDebug
        {
            get { return _loggerDebug; }
        }
        /// <summary>日志保存器对象(Warn)</summary>
        private static readonly LogSaver _loggerWarn;
        /// <summary>日志保存器对象(Warn)</summary>
        public static LogSaver LoggerWarn
        {
            get { return _loggerWarn; }
        }
        /// <summary>日志保存器对象(Error)</summary>
        private static readonly LogSaver _loggerError;
        /// <summary>日志保存器对象(Error)</summary>
        public static LogSaver LoggerError
        {
            get { return _loggerError; }
        }
        /// <summary>日志保存器对象(Fatal)</summary>
        private static readonly LogSaver _loggerFatal;
        /// <summary>日志保存器对象(Fatal)</summary>
        public static LogSaver LoggerFatal
        {
            get { return _loggerFatal; }
        }
// ReSharper restore InconsistentNaming

        /// <summary>
        /// Log序列号计数器
        /// </summary>
        private static long _logSn = -1;
        #endregion

        #region 方法
        /// <summary>
        /// 保存日志
        /// </summary>
        /// <param name="logContent">日志内容</param>
        public static void Log(string logContent)
        {
            Logger.Log(LogType.INFO, logContent);
        }
        /// <summary>
        /// 保存日志
        /// </summary>
        /// <param name="logType">日志类型</param>
        /// <param name="logContent">日志内容</param>
        public static void Log(string logType, string logContent)
        {
            LogSaver ls;
            switch (logType.ToUpper().Trim())
            {
                case "WARN":
                    ls = LoggerWarn;
                    break;
                case "ERROR":
                    ls = LoggerError;
                    break;
                case "FATAL":
                    ls = LoggerFatal;
                    break;
                case "DEBUG":
                    ls = LoggerDebug;
                    break;
                case "Info":
                    ls = Logger;
                    break;
                default:
                    ls = Logger;
                    break;
            }
            ls.Log(logType, logContent);
        }

        /// <summary>
        /// 控制台输出日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Cow(string message)
        {
            Console.Out.WriteLine("{0} [Info] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);
        }
        /// <summary>
        /// 控制台输出日志
        /// </summary>
        /// <param name="logType">日志类型</param>
        /// <param name="message">日志消息</param>
        public static void Cow(string logType, string message)
        {
            Console.Out.WriteLine("{0} [{1}] {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), logType, message);
        }
        /// <summary>
        /// 控制台输出日志，并同时写日志文件
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void CowLog(string message)
        {
            Log(message);
            Cow(message);
        }
        /// <summary>
        /// 控制台输出日志，并同时写日志文件
        /// </summary>
        /// <param name="logType">日志类型</param>
        /// <param name="message">日志消息</param>
        public static void CowLog(string logType, string message)
        {
            Log(logType, message);
            Cow(logType, message);
        }

        /// <summary>
        /// 获取Log序列号计数的新值
        /// </summary>
        /// <returns>格式化为8位</returns>
        public static string GetLogSn()
        {
            if (_logSn < 0)
            {
                try
                {
                    if (!File.Exists(LogSnConfigFile))
                        File.WriteAllText(LogSnConfigFile, "0");
                    string s = File.ReadAllText(LogSnConfigFile);
                    Log("Info", string.Format("[Config.GetLogSn] 从持久化文件中加载Log序列号。[LogSnConfigFile={0}] [LogSn={1}]", LogSnConfigFile, s));
                    bool b = long.TryParse(s, out _logSn);
                    if (!b)
                        _logSn = 0;
                }
                catch (Exception err)
                {
                    Log("Fatal", string.Format("[Config.GetLogSn] 读取Log序列号时发生异常。[LogSnConfigFile={0}]异常信息：{1}\r\n堆栈：{2}", LogSnConfigFile, err.Message, err.StackTrace));
                }
            }
            long sn = Interlocked.Increment(ref _logSn);
            return sn.ToString("X8");
        }
        /// <summary>
        /// 将Log序列号持久化保存
        /// </summary>
        public static void SaveLogSn()
        {
            try
            {
                if (_logSn == -1)
                    return;
                File.WriteAllText(LogSnConfigFile, _logSn.ToString(CultureInfo.InvariantCulture));
                Log("Info", string.Format("[Config.SaveLogSn] 向持久化文件中保存Log序列号。[LogSnConfigFile={0}] [LogSn={1}]", LogSnConfigFile, _logSn));
            }
            catch (Exception err)
            {
                Log("Fatal", string.Format("[Config.SaveLogSn] 持久化保存Log序列号时发生异常。[LogSnConfigFile={0}]异常信息：{1}\r\n堆栈：{2}", LogSnConfigFile, err.Message, err.StackTrace));
            }
        }
        #endregion
        #endregion

        #region 配置文件相关
        #region 常量
        /// <summary>Config文件路径</summary>
        private const string ConfigFile = "Config.xml";
        #endregion

        #region 属性
        /// <summary>
        /// 版本号
        /// </summary>
        public static string Version
        {
            get { return Application.ProductVersion; }
        }

        /// <summary>程序启动运行路径(不包含末尾的反斜杠)</summary>
        private static string _appPath = string.Empty;
        /// <summary>程序启动运行路径(不包含末尾的反斜杠)</summary>
        public static string AppPath
        {
            get
            {
                if (string.IsNullOrEmpty(_appPath))
                {
                    _appPath = AppDomain.CurrentDomain.BaseDirectory;
                    if (_appPath.Substring(_appPath.Length - 1, 1) == @"\")
                        _appPath = _appPath.Substring(0, _appPath.Length - 1);
                }
                return _appPath;
            }
        }

        private static int _pId;
        /// <summary>
        /// 进程ID
        /// </summary>
        public static int PId
        {
            get
            {
                if (_pId == 0)
                {
                    _pId = Process.GetCurrentProcess().Id;
                    Log("Info", string.Format("[Config.PId.get] 当前进程的进程PID为[{0}]。", _pId));
                }
                return _pId;
            }
        }

        /// <summary>配置XML文档实例</summary>
        private static XmlDocument _domConfig;
        /// <summary>配置XML文档实例</summary>
        public static XmlDocument DomConfig
        {
            get
            {
                try
                {
                    if (_domConfig == null)
                    {
                        _domConfig = new XmlDocument();
                        _domConfig.Load(string.Format("{0}\\{1}", AppPath, ConfigFile));
                    }
                }
                catch (Exception err)
                {
                    var msg = string.Format("[Config.DomConfig.get] 致命的错误。加载配置文件失败。请检查文件[{0}\\{1}]格式是否正确。", AppPath, ConfigFile);
                    Cow("FATAL", msg);
                    Log("FATAL", string.Format("{0}异常信息：{1}\r\n堆栈：{2}", msg, err.Message, err.StackTrace));
                }
                return _domConfig;
            }
        }
        #endregion
        #endregion

        #region 基本配置相关
        #region 常数
        /// <summary>默认配置：应用程序名称</summary>
        private const string DefaultProgramname = "标准服务程序";
        /// <summary>XML配置节：应用程序名称</summary>
        private const string PathProgramname = "//root/HCXT.App.StandardService/ProgramName";
        /// <summary>XML配置节：默认窗体图标文件名</summary>
        private const string PathIcon = "//root/HCXT.App.StandardService/Icon";
        /// <summary>XML配置节：是否进程互斥（以窗体名称为判断依据）</summary>
        private const string PathAlone = "//root/HCXT.App.StandardService/Alone";
        #endregion

        #region 私有成员变量
        /// <summary>应用程序名称</summary>
        private static string _programName;
        /// <summary>默认窗体图标文件名</summary>
        private static string _icon;
        /// <summary>是否进程互斥（以窗体名称为判断依据）</summary>
        private static bool _alone;
        #endregion

        #region 属性
        /// <summary>应用程序名称</summary>
        public static string ProgramName
        {
            get
            {
                if (string.IsNullOrEmpty(_programName))
                    _programName = GetStringlValueFromDom(PathProgramname, DefaultProgramname);
                return _programName;
            }
        }
        /// <summary>默认窗体图标文件名</summary>
        public static string Icon
        {
            get
            {
                if (string.IsNullOrEmpty(_icon))
                    _icon = GetStringlValueFromDom(PathIcon, "App.ico");
                return _icon;
            }
        }
        /// <summary>是否进程互斥（以窗体名称为判断依据）</summary>
        public static bool Alone
        {
            get
            {
                var s = GetStringlValueFromDom(PathAlone, "False");
                _alone = ConfigValue2Bool(s);
                return _alone;
            }
        }
        #endregion
        #endregion

        #region 应用相关
        #region 常数
        const string PathServices = "//root/HCXT.App.StandardService/Services";
        #endregion

        #region 私有成员变量
        // ReSharper disable InconsistentNaming
        private static string[] _services;
        // ReSharper restore InconsistentNaming
        #endregion

        #region 属性
        /// <summary>
        /// 服务名称数组
        /// </summary>
        public static string[] Services
        {
            get { return _services; }
        }

        #endregion
        #endregion

        #region 工具方法
        /// <summary>
        /// 从指定XML节点获取InnerText值
        /// </summary>
        /// <param name="xmlPath"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetStringlValueFromDom(string xmlPath, string defaultValue)
        {
            string rersult = defaultValue;
            try
            {
                var selectSingleNode = DomConfig.SelectSingleNode(xmlPath);
                if (selectSingleNode != null)
                    rersult = selectSingleNode.InnerText.Trim();
            }
            catch (Exception err)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(1);
                Log("ERROR",
                    string.Format("[AppConfig.{0}] 从XML中读取配置段[{1}]错误。消息：{2}\r\n堆栈：{3}", sf.GetMethod().Name, xmlPath,
                                  err.Message, err.StackTrace));
            }
            return rersult;
        }

        /// <summary>
        /// 将配置文件中的配置项文本(Y/Yes/1/True)转化成为布尔值
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns></returns>
        public static bool ConfigValue2Bool(string strValue)
        {
            string str = strValue.Trim().ToLower();
            return str == "y" || str == "yes" || str == "1" || str == "true";
        }

        /// <summary>
        /// 将配置文件中的配置项文本(Y/Yes/1/True)转化成为布尔值
        /// </summary>
        /// <param name="strValue">配置文件键值</param>
        /// <param name="defauleIsFalse">是否默认为假（即，凡是不能匹配成真值的，全按假值来算，反之亦然）</param>
        /// <returns></returns>
        public static bool ConfigValue2Bool(string strValue, bool defauleIsFalse)
        {
            var str = strValue.Trim().ToLower();
            if (defauleIsFalse)
            {
                return str == "y" || str == "yes" || str == "1" || str == "true";
            }
            return !(str == "n" || str == "no" || str == "0" || str == "false");
        }

        /// <summary>
        /// 检查当前操作系统中是否有指定名称的进程在运行中
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public static bool FindProcess(params string[] processName)
        {
            var arrP = Process.GetProcesses();
            return arrP.Select(p => p.ProcessName.Trim().ToLower()).Any(pName => processName.Any(n => pName == n.ToLower()));
        }

        /// <summary>
        /// 将iso-8859-1编码的字符串转换成gb18030编码的字符串
        /// </summary>
        /// <param name="strIso">iso-8859-1编码的字符串</param>
        /// <returns>gb18030编码的字符串</returns>
        public static string Iso2Gb(string strIso)
        {
            if (string.IsNullOrEmpty(strIso)) return "";
            var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(strIso);
            return Encoding.GetEncoding("gb18030").GetString(bytes);
        }
        /// <summary>
        /// 将gb18030编码的字符串转换成iso-8859-1编码的字符串
        /// </summary>
        /// <param name="strGb">gb18030编码的字符串</param>
        /// <returns>iso-8859-1编码的字符串</returns>
        public static string Gb2Iso(string strGb)
        {
            if (string.IsNullOrEmpty(strGb)) return "";
            var bytes = Encoding.GetEncoding("gb18030").GetBytes(strGb);
            return Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        }
        #endregion
    }
}
