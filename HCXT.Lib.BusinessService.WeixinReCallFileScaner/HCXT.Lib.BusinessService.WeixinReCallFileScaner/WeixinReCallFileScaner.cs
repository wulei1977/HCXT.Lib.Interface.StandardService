using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using HCXT.Lib.BusinessService.WeixinReCallFileScaner.Service_MallService;
using HCXT.Lib.Interface.StandardService;
using HCXT.Lib.WeiXinPay.Model;

namespace HCXT.Lib.BusinessService.WeixinReCallFileScaner
{
    // ReSharper disable ConvertToAutoProperty
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// 微信支付回调文件扫描服务类库
    /// </summary>
    public class WeixinReCallFileScaner : IBusinessService
    {
        #region 实现接口IBusinessService
        private string _serviceName;
        private string _description;
        private int _timeOut = 60000;
        private int _timeSpan;
        private bool _enabled;
        private bool _isRunning;
        private string _serviceConfigString;
        private object _otherArgs;

        /// <summary>启动服务</summary>
        public void Start()
        {
            const string logHead = "[WeixinReCallFileScaner.Start] ";
            thread = new Thread(ThreadMethod) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Log("Info", string.Format("{0}服务[{1}]主线程已启动。", logHead, _serviceName));
        }
        /// <summary>停止服务</summary>
        public void Stop()
        {
            const string logHead = "[WeixinReCallFileScaner.Stop] ";
            try
            {
                _isRunning = false;
                thread.Abort();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
            Log("Info", string.Format("{0}服务[{1}]主线程已停止。", logHead, _serviceName));
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
                const string logHead = "[WeixinReCallFileScaner.ServiceConfigString.set] ";
                #region 传入的服务配置XML串格式示例
                /*
  <HCXT.Lib.BusinessService.WeixinReCallFileScaner>
    <!--服务名称-->
    <ServiceName>HCXT.Lib.BusinessService.WeixinReCallFileScaner</ServiceName>
    <!--服务中文名称-->
    <Description>微信支付回调文件扫描服务</Description>
    <!--服务是否允许启用-->
    <Enabled>true</Enabled>
    <!--扫描步长时间（毫秒）-->
    <TimeSpan>2000</TimeSpan>
    <!--上行支付结果回调文件目录-->
    <PathScan>D:\log\WeiXinMessageUp</PathScan>
    <!--上行支付结果回调文件处理后移往目录-->
    <PathSucc>D:\log\WeiXinMessageUp\Succ</PathSucc>
    <!--上行支付结果回调文件处理异常后移往目录-->
    <PathError>D:\log\WeiXinMessageUp\Error</PathError>
    <!--上行支付结果回调文件筛选通配符-->
    <FilterScan>MsgReCall_*.txt</FilterScan>
    <!--电渠核心地址-->
    <ServerIp>192.168.2.13</ServerIp>
    <!--电渠核心微信支付回调服务端口-->
    <Port>45200</Port>
    <!--API密钥 Key-->
    <Key>12a59cebdac7521561cc8ca00822e079</Key>
    <!--商城接口WebService地址URL-->
    <Service_MallService_Url>http://gd.cttgd.com:8067/MallService.asmx</Service_MallService_Url>
  </HCXT.Lib.BusinessService.WeixinReCallFileScaner>
                 */
                #endregion
                try
                {
                    var dom = new XmlDocument();
                    dom.LoadXml(value);
                    const string sRoot = "//HCXT.Lib.BusinessService.WeixinReCallFileScaner/";
                    var arrName = new[] { "ServiceName", "Description", "Enabled", "TimeSpan", "PathScan", "PathSucc", "PathError", "FilterScan", "ServerIp", "Port", "TimeOut", "Key", "Service_MallService_Url" };
                    foreach (var pName in arrName)
                    {
                        var node = dom.SelectSingleNode(sRoot + pName);
                        if (node == null || string.IsNullOrWhiteSpace(node.InnerText))
                            throw new Exception(string.Format("配置文件节点[{0}]未取到值！", pName));
                        var p = typeof(WeixinReCallFileScaner).GetProperty(pName);
                        Log("Debug", string.Format("{0}正在配置[{1}]({2})属性。", logHead, pName, p.PropertyType.Name));
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
                            case "String":
                            default:
                                p.SetValue(this, node.InnerText, null);
                                break;
                        }
                    }
                }
                catch (Exception err)
                {
                    Log("Fatal", string.Format("{0}发生致命的异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
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

        private string _pathScan;
        /// <summary>上行支付结果回调文件目录</summary>
        public string PathScan
        {
            get { return _pathScan; }
            set { _pathScan = value; }
        }

        private string _pathSucc;
        /// <summary>上行支付结果回调文件处理后移往目录</summary>
        public string PathSucc
        {
            get { return _pathSucc; }
            set { _pathSucc = value; }
        }

        private string _pathError;
        /// <summary>上行支付结果回调文件处理异常后移往目录</summary>
        public string PathError
        {
            get { return _pathError; }
            set { _pathError = value; }
        }

        private string _filterScan;
        /// <summary>上行支付结果回调文件筛选通配符</summary>
        public string FilterScan
        {
            get { return _filterScan; }
            set { _filterScan = value; }
        }

        private string _serverIp;
        /// <summary>
        /// 核心服务端Ip地址
        /// </summary>
        public string ServerIp
        {
            get { return _serverIp; }
            set { _serverIp = value; }
        }

        private int _port;
        /// <summary>
        /// 核心服务端端口
        /// </summary>
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        private string _service_MallService_Url;
        /// <summary>
        /// 商城接口WebService地址URL
        /// </summary>
        public string Service_MallService_Url
        {
            get { return _service_MallService_Url; }
            set { _service_MallService_Url = value; }
        }

        private string _key;
        /// <summary>
        /// API密钥 Key
        /// </summary>
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        /// <summary>服务主线程对象</summary>
        private Thread thread;
        /// <summary>
        /// 服务主线程方法
        /// </summary>
        private void ThreadMethod()
        {
            const string logHead = "[WeixinReCallFileScaner.ThreadMethod] ";
            _isRunning = true;
            while (_isRunning)
            {
                try
                {
                    //Directory.GetFiles(_pathScan, _filterScan);
                    var directory = new DirectoryInfo(_pathScan);
                    var files = directory.GetFileSystemInfos(_filterScan);
                    for (var i = 0; i < files.Length; i++)
                    {
                        var fileName = files[i].Name;
                        var fullName = files[i].FullName;
                        string txt;//文件文本串
                        try
                        {
                            txt = File.ReadAllText(fullName, Encoding.UTF8);
                        }
                        catch (Exception err)
                        {
                            Log("Error", string.Format("{0}读取文件[{1}]异常。异常信息：\r\n{2}堆栈：{3}", logHead, fullName, err.Message, err.StackTrace));
                            continue;
                        }
                        try
                        {
                            var obj = DeSerializeXml(txt, typeof(RequestReCallNotify), new RequestReCallNotify());
                            if (obj == null)
                                throw new Exception("反序列化上行消息请求包失败。");
                            var mod = (RequestReCallNotify)obj;

                            // 判断签名是否合法
                            var sign = XmlSign(txt, Key);
                            if (string.IsNullOrEmpty(sign) || mod.sign != sign)
                            {
                                Log("WARN", string.Format("{0}警告：微信支付回调文件[{1}]的签名验证未通过。该文件很可能是经过篡改的非法回调记录，请注意。", logHead, fileName));
                                File.Move(fullName, _pathError + "\\" + fileName);
                                continue;
                            }

                            if (mod.return_code == "SUCCESS")
                            {
                                // 先判断是否有私有域参数，如果没有私有域参数，则直接视为核心回调
                                // attach的格式为： 回调业务方向(核心eproxy或商城mall)|paymentSn|支付金额（元）|支付结果(SUCCESS)|产品3|区号
                                if (!string.IsNullOrEmpty(mod.attach)) // 
                                {
                                    Log("Info",string.Format("{0}私有域内容为：[{1}]", logHead, mod.attach));
                                    var arrAttach = mod.attach.Split(new[] {'|'}, StringSplitOptions.None);
                                    if (arrAttach[0] == "mall") //商城
                                    {
                                        Log("Info", string.Format("{0}该回调为商城回调。准备调用WebService方法[UpdateEBuayOrder]。URL：[{1}]", logHead, Service_MallService_Url));
                                        var arg = string.Join("|", arrAttach, 1, arrAttach.Length - 1);
                                        var service = new MallService { Url = Service_MallService_Url };
                                        try
                                        {
                                            var res = service.UpdateEBuayOrder(arg);// 返回0表示失败，其他成功
                                            Log("Info", string.Format("{0}商城回调WebService方法返回值：[{1}]", logHead, res));
                                            if (res == "0")
                                                throw new Exception("商城WebService回调方法返回值为[0]，表示失败。");
                                            File.Move(fullName, _pathSucc + "\\" + fileName);
                                        }
                                        catch (Exception err)
                                        {
                                            Log("Error",string.Format("{0}异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                                            File.Move(fullName, _pathError + "\\" + fileName);
                                        }
                                        finally
                                        {
                                            service.Dispose();
                                        }
                                        continue; // 这里结束本轮循环
                                    }
                                }

                                var requestSn = DateTime.Now.ToString("yyyyMMddHHmmssfff") + new Random().Next(100, 999);
                                var payResult = mod.result_code == "SUCCESS" ? "1" : "0";// 1成功 0失败
                                var xml = new StringBuilder();
                                xml.Append("<Proxy>");
                                xml.Append("<RequestSn>" + requestSn + "</RequestSn>");
                                xml.Append("<Service code=\"req.pay.return\" from=\"weixin\">");
                                xml.Append("<PaymentSn>" + mod.out_trade_no + "</PaymentSn>");
                                xml.Append("<PaymentMoney>" + mod.total_fee + "</PaymentMoney>");
                                xml.Append("<PaymentYlSn>" + mod.transaction_id + "</PaymentYlSn>"); //微信支付订单号 transaction_id
                                xml.Append("<Result>" + payResult + "</Result>");// 1成功 0失败
                                xml.Append("<ResultRemark></ResultRemark>");
                                xml.Append("<PayErrorCode>" + mod.err_code + "</PayErrorCode>");// 自定义
                                xml.Append("</Service></Proxy>");

                                string result;
                                var tcpQueryer = new TcpQueryer
                                {
                                    ServerIP = ServerIp,
                                    ServerPort = Port,
                                    TimeOut = TimeOut
                                };
                                tcpQueryer.OnLog += tcpQueryer_OnLog;
                                var resultLen = tcpQueryer.Query(xml.ToString(), out result);
                                if (resultLen <= 0)
                                    throw new Exception("向核心发送数据包失败。");
                                Log("Info", string.Format("{0}核心返回包：{1}", logHead, result));
                            }
                            File.Move(fullName, _pathSucc + "\\" + fileName);
                        }
                        catch (Exception err)
                        {
                            Log("Error", string.Format("{0}文件[{1}]解析错误。异常信息：\r\n{2}堆栈：{3}", logHead, fullName, err.Message, err.StackTrace));
                            File.Move(fullName, _pathError + "\\" + fileName);// todo: ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                        }
                    }
                }
                catch (Exception err)
                {
                    Log("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                }
                Thread.Sleep(_timeSpan);
            }
        }

        void tcpQueryer_OnLog(string logType, string logMsg)
        {
            Log(logType, logMsg);
        }

        private void Log(string logType, string logMsg)
        {
            if (BusinessInvoke != null)
                BusinessInvoke("log", logType, logMsg);
        }


        /// <summary>
        /// 将Xml反序列化为指定类型的模型类实例
        /// </summary>
        /// <param name="xml">传入的Xml</param>
        /// <param name="type">指定的类型</param>
        /// <param name="obj">传入的要填充的模型类实例</param>
        /// <returns></returns>
        private object DeSerializeXml(string xml, Type type, object obj)
        {
            const string logHead = "[WeixinReCallFileScaner.DeSerializeXml] ";
            try
            {
                var dom = new XmlDocument();
                dom.LoadXml(xml);
                var root = dom.SelectSingleNode("//xml");
                if (root == null)
                {
                    Log("Info", string.Format("{0}反序列化模型类的时候发生异常。传入的Xml中未发现[//xml]节点。", logHead));
                    return null;
                }
                foreach (XmlNode node in root.ChildNodes)
                {
                    var p = type.GetProperty(node.Name);
                    if (p == null || node.FirstChild == null)
                        continue;
                    var vStr = node.FirstChild.Value;
                    switch (p.PropertyType.Name)
                    {
                        case "Int32":
                            int vInt;
                            int.TryParse(vStr, out vInt);
                            p.SetValue(obj, vInt, null);
                            break;
                        case "DateTime":
                            // 目前没有此类型，尚不知文本是何种格式
                            break;
                        case "String":
                        default:
                            p.SetValue(obj, vStr, null);
                            break;
                    }
                }
                return obj;
            }
            catch (Exception err)
            {
                Log("Info", string.Format("{0}反序列化模型类的时候发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
            }
            return null;
        }


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
        /// 验证签名
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private string XmlSign(string xml, string key)
        {
            const string logHead = "[WeixinReCallFileScaner.XmlSign] ";
            try
            {
                var dom = new XmlDocument();
                dom.LoadXml(xml);
                var root = dom.SelectSingleNode("//xml");
                if (root == null)
                {
                    Log("Info", string.Format("{0}发生异常。传入的Xml中未发现[//xml]节点。", logHead));
                    return null;
                }
                var list = new List<string>();
                for (var i = 0; i < root.ChildNodes.Count; i++)
                {
                    var node = root.ChildNodes[i];
                    // 如果参数的值为空不参与签名；
                    // 验证调用返回或微信主动通知签名时，传送的sign参数不参与签名，将生成的签名与该sign值作校验。
                    if (node.Name.ToLower() == "sign" || node.FirstChild == null || string.IsNullOrWhiteSpace(node.FirstChild.Value))
                        continue;
                    list.Add(string.Format("{0}={1}", node.Name, node.FirstChild.Value));
                }
                var arr = list.ToArray();
                Array.Sort(arr);
                var stringA = string.Join("&", arr);
                Log("Debug", string.Format("{0}#1.生成字符串：{1}", logHead, stringA));
                var s = string.Format("{0}&key={1}", stringA, key);
                Log("Debug", string.Format("{0}#2.连接商户key：{1}", logHead, s));
                var sign = GetMd5(s, false, "utf-8").ToUpper();
                Log("Debug", string.Format("{0}#3.md5编码并转成大写：{1}", logHead, sign));
                return sign;
            }
            catch (Exception err)
            {
                Log("Info", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
            }
            return "";
        }
        /// <summary>
        /// 获取指定字符串的MD5编码散列字符串
        /// </summary>
        /// <param name="inputString">指定字符串</param>
        /// <param name="isFile">指定字符串是否为文件名</param>
        /// <param name="encodingName">指定字符串的编码字符集(当isFile==false时生效)</param>
        /// <returns></returns>
        private static string GetMd5(string inputString, bool isFile, string encodingName)
        {
            // 日志头
            const string logHead = "[WeixinReCallFileScaner.GetMd5] ";
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
    }
    // ReSharper restore InconsistentNaming
    // ReSharper restore ConvertToAutoProperty
}
