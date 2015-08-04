using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using HCXT.Lib.DB.DBAccess;
using HCXT.Lib.Interface.StandardService;
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToAutoProperty
// ReSharper disable ConvertToAutoPropertyWhenPossible

namespace HCXT.Lib.BusinessService.GdLottery
{
    /// <summary>
    /// 
    /// </summary>
    public class GdLottery : IBusinessService
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
            const string logHead = "[GdLottery.Start] ";
            _threadLottery = new Thread(ThreadMethodLottery) { IsBackground = true };
            _threadLottery.SetApartmentState(ApartmentState.STA);
            _threadLottery.Start();
            _threadVerification = new Thread(ThreadMethodVerification) { IsBackground = true };
            _threadVerification.SetApartmentState(ApartmentState.STA);
            _threadVerification.Start();

            _threadGc = new Thread(GcThreadMethod) { IsBackground = true };
            _threadGc.SetApartmentState(ApartmentState.STA);
            _threadGc.Start();
            Cow("Info", string.Format("{0}服务[{1}]主线程已启动。", logHead, _serviceName));
        }
        /// <summary>停止服务</summary>
        public void Stop()
        {
            const string logHead = "[GdLottery.Stop] ";
            _isRunning = false;
            Cow("Info", string.Format("{0}服务线程正在停止，请稍候。", logHead));
            try
            {
                _threadLottery.Abort();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
            try
            {
                _threadVerification.Abort();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
            try
            {
                _threadGc.Abort();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
            Thread.Sleep(2000);
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
                const string logHead = "[GdLottery.ServiceConfigString.set] ";
                #region 传入的服务配置XML串格式示例
                /*
<root>
  <HCXT.Lib.BusinessService.GdLottery>
    <!--服务名称-->
    <ServiceName>HCXT.Lib.BusinessService.GdLottery</ServiceName>
    <!--服务中文名称-->
    <Description>广东电渠微信营业厅抽奖服务</Description>
    <!--服务是否允许启用-->
    <Enabled>true</Enabled>
    <!--扫描步长时间（毫秒）-->
    <TimeSpan>2000</TimeSpan>
    <!--数据库连接类型(MSSQL)-->
    <ConnType>mssql</ConnType>
    <!--数据库连接串-->
    <ConnString>server=(local);Initial Catalog=EC;uid=tuangou;pwd=hcxt4007</ConnString>
    <!--通讯加密私钥-->
    <PrivateKey>1234567890123456</PrivateKey>
    
    <!--抽奖码库接口监听绑定IP地址-->
    <HttpListenIP_Lottery>mssql</HttpListenIP_Lottery>
    <!--抽奖码库接口监听端口-->
    <HttpListenPort_Lottery>mssql</HttpListenPort_Lottery>
    <!--兑奖接口监听绑定IP地址-->
    <HttpListenIP_Verification>mssql</HttpListenIP_Verification>
    <!--兑奖接口监听端口-->
    <HttpListenPort_Verification>mssql</HttpListenPort_Verification>
  
  </HCXT.Lib.BusinessService.GdLottery>
</root>
                 */
                #endregion
                try
                {
                    var dom = new XmlDocument();
                    dom.LoadXml(value);
                    const string sRoot = "//HCXT.Lib.BusinessService.GdLottery/";
                    var arrName = new[]
                    {
                        "ServiceName", "Description", "Enabled", "TimeSpan", "ConnType", "ConnString", "PrivateKey",
                        "HttpListenIP_Lottery", "HttpListenPort_Lottery", "HttpListenIP_Verification",
                        "HttpListenPort_Verification", "IpSecurityPolicy_Enabled", "IpSecurityPolicy_IpList",
                        "EProxy_Ip", "EProxy_Port", "SmsTemplate", "DefaultBatch"
                    };
                    foreach (var pName in arrName)
                    {
                        var node = dom.SelectSingleNode(sRoot + pName);
                        if (node == null || string.IsNullOrWhiteSpace(node.InnerText))
                            throw new Exception(string.Format("配置文件节点[{0}]未取到值！", pName));
                        var p = typeof(GdLottery).GetProperty(pName);
                        Cow("Debug", string.Format("{0}正在配置[{1}]({2})属性。", logHead, pName, p.PropertyType.Name));
                        switch (p.PropertyType.Name)
                        {
                            case "Int32":
                                int vInt;
                                int.TryParse(node.InnerText, out vInt);
                                p.SetValue(this, vInt, null);
                                break;
                            case "Boolean":
                                p.SetValue(this, ConfigValue2Bool(node.InnerText), null);
                                break;
                            //case "String":
                            default:
                                p.SetValue(this, node.InnerText, null);
                                break;
                        }
                    }
                    Thread.Sleep(10);
                }
                catch (Exception err)
                {
                    Cow("Fatal", string.Format("{0}发生致命的异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                }
                _serviceConfigString = value;
            }
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

        /// <summary>连接类型</summary>
        public string ConnType { get; set; }

        /// <summary>数据库连接串</summary>
        public string ConnString { get; set; }

        /// <summary>通讯加密私钥</summary>
        public string PrivateKey { get; set; }
        /// <summary>抽奖码库接口监听绑定IP地址</summary>
        public string HttpListenIP_Lottery { get; set; }
        /// <summary>抽奖码库接口监听端口</summary>
        public string HttpListenPort_Lottery { get; set; }
        /// <summary>兑奖接口监听绑定IP地址</summary>
        public string HttpListenIP_Verification { get; set; }
        /// <summary>兑奖接口监听端口</summary>
        public string HttpListenPort_Verification { get; set; }

        /// <summary>是否启用Ip安全策略</summary>
        public bool IpSecurityPolicy_Enabled { get; set; }
        /// <summary>白名单IP地址列表，用半角逗号分隔</summary>
        public string IpSecurityPolicy_IpList { get; set; }
        /// <summary>电渠核心程序IP地址</summary>
        public string EProxy_Ip { get; set; }
        /// <summary>电渠核心程序监听端口</summary>
        public string EProxy_Port { get; set; }
        /// <summary>短信模版</summary>
        public string SmsTemplate { get; set; }
        /// <summary>默认批次</summary>
        public string DefaultBatch { get; set; }


        /// <summary>抽奖侧(对内)监听线程运行标志</summary>
        bool _listenFlag;
        /// <summary>抽奖侧(对内)监听线程对象</summary>
        private Thread _threadLottery;
        /// <summary>抽奖侧(对内)HTTP监听器对象</summary>
        private HttpListener _listenerLottery = null;
        /// <summary>
        /// 抽奖侧(对内)监听线程方法
        /// </summary>
        private void ThreadMethodLottery()
        {
            // 日志头
            const string logHead = "[GdLottery.ThreadMethodLottery] ";

            _listenerLottery = new HttpListener();
            _listenerLottery.Prefixes.Add(string.Format("http://{0}:{1}/", HttpListenIP_Lottery, HttpListenPort_Lottery));
            _listenerLottery.Start();
            Cow("Info", string.Format("{0}抽奖侧(对内)开始监听来自端口[{1}]的HTTP回调请求", logHead, HttpListenPort_Lottery));
            _listenFlag = true;
            while (_listenFlag)
            {
                try
                {
                    var context = _listenerLottery.GetContext();
                    new LotteryListenOpt(context, this);
                }
                catch (Exception err)
                {

                }
            }
            _listenerLottery.Close();
            _listenerLottery = null;
            _listenFlag = false;
            Cow("Info", string.Format("{0}服务线程方法已结束", logHead));
        }
        /// <summary>兑奖侧(对外)监听线程对象</summary>
        private Thread _threadVerification;
        /// <summary>兑奖侧(对外)HTTP监听器对象</summary>
        private HttpListener _listenerVerification = null;
        /// <summary>
        /// 兑奖侧(对外)监听线程方法
        /// </summary>
        private void ThreadMethodVerification()
        {
            // 日志头
            const string logHead = "[GdLottery.ThreadMethodVerification] ";

            _listenerVerification = new HttpListener();
            _listenerVerification.Prefixes.Add(string.Format("http://{0}:{1}/", HttpListenIP_Verification, HttpListenPort_Verification));
            _listenerVerification.Start();
            Cow("Info", string.Format("{0}兑奖侧(对外)开始监听来自端口[{1}]的HTTP回调请求", logHead, HttpListenPort_Verification));
            _listenFlag = true;
            while (_listenFlag)
            {
                try
                {
                    var context = _listenerVerification.GetContext();
                    new VerificationListenOpt(context, this);
                }
                catch (Exception err)
                {

                }
            }
            _listenerVerification.Close();
            _listenerVerification = null;
            _listenFlag = false;
            Cow("Info", string.Format("{0}服务线程方法已结束", logHead));
        }

        #region 工具方法
        /// <summary>
        /// 将配置文件中的配置项文本(Y/Yes/1/True)转化成为布尔值
        /// </summary>
        /// <param name="strValue"></param>
        /// <returns></returns>
        private bool ConfigValue2Bool(string strValue)
        {
            string str = strValue.Trim().ToLower();
            return str == "y" || str == "yes" || str == "1" || str == "true";
        }
        /// <summary>
        /// 将iso-8859-1编码的字符串转换成gb2312编码的字符串
        /// </summary>
        /// <param name="strIso">iso-8859-1编码的字符串</param>
        /// <returns>gb2312编码的字符串</returns>
        public string Iso2Gb(string strIso)
        {
            if (string.IsNullOrEmpty(strIso))
                return "";
            var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(strIso);
            return Encoding.GetEncoding("gb2312").GetString(bytes);
        }
        /// <summary>
        /// 将gb2312编码的字符串转换成iso-8859-1编码的字符串
        /// </summary>
        /// <param name="strGb">gb2312编码的字符串</param>
        /// <returns>iso-8859-1编码的字符串</returns>
        public string Gb2Iso(string strGb)
        {
            if (string.IsNullOrEmpty(strGb))
                return "";
            var bytes = Encoding.GetEncoding("gb2312").GetBytes(strGb);
            return Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        }
        /// <summary>
        /// 检测指定的IP地址是否符合Ip策略
        /// </summary>
        /// <param name="ip">指定的IP地址</param>
        /// <returns></returns>
        public bool CheckIp(string ip)
        {
            // todo : 这里判断Ip策略+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="argNames"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public Hashtable GetRequestArgs(string[] argNames, HttpListenerRequest request)
        {
            const string logHead = "[GdLottery.GetRequestArgs] ";
            Hashtable result = new Hashtable();
            // Get方式
            if (request.HttpMethod.ToLower() == "get")
            {
                //如果参数数量不对，直接返回空
                if (request.QueryString.Count != argNames.Length)
                    return null;
                try
                {
                    // 参数完整性验证
                    string[] arrQs = request.QueryString.AllKeys.ToArray();
                    foreach (string s in argNames)
                    {
                        bool finded = false;
                        foreach (var sq in arrQs.Where(sq => sq == s))
                            finded = true;
                        if (!finded)
                        {
                            Log("Warn", string.Format("{0}发生异常。GET请求参数不全。[{1}] 实际要求必须具备的参数为[{2}]", logHead, string.Join(",", arrQs), string.Join(",", argNames)));
                            return null;
                        }
                        result.Add(s, request.QueryString[s]);
                    }
                }
                catch (Exception err)
                {
                    Log("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                    return null;
                }
            }
            else // Post方式
            {
                var buff = new byte[0x10000];//65536
                var len = request.InputStream.Read(buff, 0, buff.Length);
                request.InputStream.Close();//todo: 这里是否需要close？++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // 原始请求串
                var src = Encoding.UTF8.GetString(buff, 0, len);
                Log("Debug",string.Format("{0}收到[post]数据串[{1}]", logHead, src));
                var arr = src.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in arr)
                {
                    char[] sC = { '=' };
                    var arrS = s.Split(sC, StringSplitOptions.None);
                    if (arrS.Length > 2)
                        for (var j = 2; j < arrS.Length; j++)
                            arrS[1] += "=";
                    result.Add(arrS[0], arrS[1]);
                }
            }
            return result;
        }
        /// <summary>
        /// 向指定的Response通道输出页面信息，并关闭Response对象
        /// </summary>
        /// <param name="code">返回码</param>
        /// <param name="response">HttpResponse对象</param>
        public void ResponsePage(string code, HttpListenerResponse response)
        {
            // 日志头
            const string logHead = "[GdLottery.ResponsePage] ";
            Log("Info", string.Format("{0}Response结果[{1}]", logHead, code));
            byte[] buffer = Encoding.UTF8.GetBytes(code);
            try
            {
                response.Close(buffer, true);
            }
            catch (Exception err)
            {
                Log("Error", string.Format("{0}Response数据时发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
            }
        }
        #endregion

        #region 日志相关方法
        private readonly object lockerConsole = new object();
        /// <summary>
        /// 通过BusinessInvoke事件扔出日志
        /// </summary>
        /// <param name="logType">日志类型(Info/Debug/Warn/Error/Fatal)</param>
        /// <param name="logMsg">日志内容文本</param>
        public void Log(string logType, string logMsg)
        {
            if (BusinessInvoke != null)
                BusinessInvoke("log", logType, logMsg);
        }
        /// <summary>
        /// 在控制台输出日志，并通过BusinessInvoke事件扔出日志
        /// </summary>
        /// <param name="logType">日志类型(Info/Debug/Warn/Error/Fatal)</param>
        /// <param name="message">日志内容文本</param>
        public void Cow(string logType, string message)
        {
            Log(logType, message);
            lock (lockerConsole)
                Console.Out.WriteLine("{0} [{1}] {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), logType, message);
        }
        #endregion

        #region MD5相关方法
        /// <summary>
        /// 获取指定字符串的MD5编码散列字符串
        /// </summary>
        /// <param name="inputString">指定字符串</param>
        /// <param name="isFile">指定字符串是否为文件名</param>
        /// <param name="encodingName">指定字符串的编码字符集(当isFile==false时生效)</param>
        /// <returns></returns>
        public static string GetMd5(string inputString, bool isFile, string encodingName)
        {
            // 日志头
            const string logHead = "[GdLottery.GetMd5] ";
            var md5 = new MD5CryptoServiceProvider();
            byte[] md5Byte;
            if (isFile)
            {
                if (File.Exists(inputString))
                {
                    FileStream fs = null;
                    try
                    {
                        fs = File.OpenRead(inputString);
                        md5Byte = md5.ComputeHash(fs);
                    }
                    catch (Exception err)
                    {
                        var errMsg = string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace);
                        throw new Exception(errMsg);
                    }
                    finally
                    {
                        if (fs != null)
                        {
                            fs.Close();
                            fs.Dispose();
                        }
                    }
                }
                else
                {
                    var errMsg = string.Format("{0}文件[{1}]不存在", logHead, inputString);
                    throw new Exception(errMsg);
                }
            }
            else
            {
                var buff = Encoding.GetEncoding(encodingName).GetBytes(inputString);
                md5Byte = md5.ComputeHash(buff);
            }
            md5.Dispose();
            var sb = new StringBuilder();
            foreach (var b in md5Byte)
            {
                var i = Convert.ToInt32(b);
                var j = i >> 4;
                sb.Append(Convert.ToString(j, 16));
                j = ((i << 4) & 0x00ff) >> 4;
                sb.Append(Convert.ToString(j, 16));
            }
            return sb.ToString();
        }
        #endregion

        #region 随机串发生器相关
        /// <summary>静态伪随机数发生器</summary>
        private readonly Random _rnd = new Random();
        /*
        /// <summary>
        /// 创建指定长度的随机串，包含数字及大小写字母
        /// </summary>
        /// <param name="count">指定字符串长度</param>
        /// <returns></returns>
        private string GetRandString(int count)
        {
            var result = new char[count];
            for (var i = 0; i < count; i++)
            {
                var r = _rnd.Next(0, 10);
                if (r < 3)
                    result[i] = Convert.ToChar(_rnd.Next(48, 58));//数字
                else if (r < 7)
                    result[i] = Convert.ToChar(_rnd.Next(65, 91));//大写
                else
                    result[i] = Convert.ToChar(_rnd.Next(97, 123));//小写
            }
            return string.Join("", result);
        }
        */
        /// <summary>
        /// 创建指定长度的随机串，只包含数字
        /// </summary>
        /// <param name="count">指定字符串长度</param>
        /// <returns></returns>
        public string GetRandNumeric(int count)
        {
            var result = new char[count];
            for (var i = 0; i < count; i++)
                result[i] = Convert.ToChar(_rnd.Next(48, 58));
            return string.Join("", result);
        }
        #endregion

        #region 定时回收内存垃圾线程相关
        private bool _isRunningGc;
        private Thread _threadGc;
        /// <summary>
        /// 定时回收内存垃圾
        /// </summary>
        private void GcThreadMethod()
        {
            const string logHead = "[GdLottery.GcThreadMethod] ";
            _isRunningGc = true;
            Log("Info", string.Format("{0}定时回收内存垃圾线程已启动。", logHead));
            long pc = 0;
            while (_isRunningGc)
            {
                Thread.Sleep(1000 * 600);
                GC.Collect(GC.MaxGeneration);
                GC.WaitForPendingFinalizers();
                Log("Info", string.Format("{0}定时回收内存垃圾线程第[{1}]次回收内存垃圾完毕。", logHead, ++pc));
                //if (Config.DbQueryExceptionCount > Config.MaxDbQueryExceptionCount)
                //{
                //    _isRunningGc = false;
                //    _isRunning = false;
                //    Config.CowLog(string.Format("[Program.GcThreadMethod] 数据库查询异常次数[{0}]已超过最大设定值[{1}]，应用程序即将退出。", Config.DbQueryExceptionCount, Config.MaxDbQueryExceptionCount));
                //}
            }
        }
        #endregion
    }
}
