using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using HCXT.Lib.BusinessService.WeixinMsgFileScaner.Ws_AccessToken;
using HCXT.Lib.DB.DBAccess;
using HCXT.Lib.Interface.StandardService;
using HCXT.Lib.WeiXinPay.Model;
using Newtonsoft.Json;
using PublicNo = HCXT.Lib.WeiXinPay.Model.PublicNo;

namespace HCXT.Lib.BusinessService.WeixinMsgFileScaner
{
    // ReSharper disable ConvertToAutoProperty
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// 微信消息文件扫描服务类库
    /// </summary>
    public class WeixinMsgFileScaner : IBusinessService
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
            const string logHead = "[WeixinMsgFileScaner.Start] ";
            thread = new Thread(ThreadMethod) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Cow("Info", string.Format("{0}服务[{1}]主线程已启动。", logHead, _serviceName));
        }
        /// <summary>停止服务</summary>
        public void Stop()
        {
            const string logHead = "[WeixinMsgFileScaner.Stop] ";
            try
            {
                _isRunning = false;
                thread.Abort();
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
                const string logHead = "[WeixinMsgFileScaner.ServiceConfigString.set] ";
                #region 传入的服务配置XML串格式示例
                /*
  <HCXT.Lib.BusinessService.WeixinMsgFileScaner>
    <!--服务名称-->
    <ServiceName>HCXT.Lib.BusinessService.WeixinMsgFileScaner</ServiceName>
    <!--服务中文名称-->
    <Description>微信上行消息文件扫描服务</Description>
    <!--服务是否允许启用-->
    <Enabled>true</Enabled>
    <!--扫描步长时间（毫秒）-->
    <TimeSpan>2000</TimeSpan>
    <!--上行一般消息文件目录-->
    <PathScan>e:\ScanUpMsg</PathScan>
    <!--上行一般消息文件处理后移往目录-->
    <PathSucc>e:\ScanUpMsg</PathSucc>
    <!--上行一般消息文件处理异常后移往目录-->
    <PathError>e:\ScanUpMsg</PathError>
    <!--上行一般消息文件筛选通配符-->
    <FilterScan>MsgUp_*.txt</FilterScan>
    <!--数据库连接类型(MSSQL)-->
    <ConnType>mssql</ConnType>
    <!--数据库连接串-->
    <ConnString>server=(local);Initial Catalog=EC;uid=tuangou;pwd=hcxt4007</ConnString>
  </HCXT.Lib.BusinessService.WeixinMsgFileScaner>
                 */
                #endregion
                try
                {
                    var dom = new XmlDocument();
                    dom.LoadXml(value);
                    const string sRoot = "//HCXT.Lib.BusinessService.WeixinMsgFileScaner/";
                    var arrName = new[] { "ServiceName", "Description", "Enabled", "TimeSpan", "PathScan", "PathSucc", "PathError", "FilterScan", "ConnType", "ConnString", "Service_AccessToken_Url", "PrivateKey" };
                    foreach (var pName in arrName)
                    {
                        var node = dom.SelectSingleNode(sRoot + pName);
                        if (node == null || string.IsNullOrWhiteSpace(node.InnerText))
                            throw new Exception(string.Format("配置文件节点[{0}]未取到值！", pName));
                        var p = typeof(WeixinMsgFileScaner).GetProperty(pName);
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
                            case "String":
                            default:
                                p.SetValue(this, node.InnerText, null);
                                break;
                        }
                    }

                    Thread.Sleep(10);

                    // 初始化数据库链接器
                    dbc = new DBConnection { ConnectionType = ConnType, ConnectionString = ConnString };
                    dbc.OnLog += Log;
                    Cow("Debug", string.Format("{0}初始化数据库链接器完成。[ConnectionType={1}][ConnectionString={2}]", logHead, ConnType, ConnString));

                    // 初始化获取公众号及AccessToken的WebService
                    WsAccessToken = new Service_AccessToken { Url = Service_AccessToken_Url };
                    Cow("Debug", string.Format("{0}初始化获取公众号及AccessToken的WebService完成。[Url={1}]", logHead, Service_AccessToken_Url));

                    // 初始化PublicNo列表
                    try
                    {
                        var rnd = Guid.NewGuid().ToString().Replace("-", "");
                        var sign = GetMd5(rnd + PrivateKey, false, "utf-8");
                        var arr = WsAccessToken.GetPublicNoList(rnd, sign);
                        if (arr != null && arr.Length > 0)
                        {
                            _PublicNoList = new List<PublicNo>();
                            foreach (var pn in arr)
                            {
                                var pN = new PublicNo();
                                var ps = typeof(PublicNo).GetProperties();
                                foreach (var p in ps)
                                {
                                    var p0 = pn.GetType().GetProperty(p.Name);
                                    var v = p0.GetValue(pn, null);
                                    p.SetValue(pN, v, null);
                                }
                                _PublicNoList.Add(pN);
                            }
                            Cow("Info", string.Format("{0}公众号列表初始化完毕。共计载入[{1}]个公众号。", logHead, _PublicNoList.Count));
                        }
                        else
                        {
                            Cow("Fatal", string.Format("{0}致命的异常。公众号列表初始化失败！！！", logHead));
                        }
                    }
                    catch (Exception err)
                    {
                        Cow("Error", string.Format("{0}获取公众号列表时发生异常。[WsAccessToken.Url={1}]异常信息：{2}\r\n堆栈：{3}", logHead, WsAccessToken.Url, err.Message, err.StackTrace));
                    }
                    /*
                    _PublicNoList = new List<PublicNo>();
                    var ds = dbc.GetDataset("SELECT * FROM PublicNo");
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        var pn = (PublicNo) DataRowToObject(dr, typeof (PublicNo), false);
                        if (pn != null)
                            _PublicNoList.Add(pn);
                    }
                    */
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


        private Service_AccessToken WsAccessToken;

        private string _pathScan;
        /// <summary>上行一般消息文件目录</summary>
        public string PathScan
        {
            get { return _pathScan; }
            set { _pathScan = value; }
        }

        private string _pathSucc;
        /// <summary>上行一般消息文件处理后移往目录</summary>
        public string PathSucc
        {
            get { return _pathSucc; }
            set { _pathSucc = value; }
        }

        private string _pathError;
        /// <summary>上行一般消息文件处理异常后移往目录</summary>
        public string PathError
        {
            get { return _pathError; }
            set { _pathError = value; }
        }

        private string _filterScan;
        /// <summary>上行一般消息文件筛选通配符</summary>
        public string FilterScan
        {
            get { return _filterScan; }
            set { _filterScan = value; }
        }

        private string _connType;
        /// <summary>连接类型</summary>
        public string ConnType
        {
            get { return _connType; }
            set { _connType = value; }
        }

        private List<PublicNo> _PublicNoList;
        /// <summary>公众号列表</summary>
        public List<PublicNo> PublicNoList
        {
            get { return _PublicNoList; }
            set { _PublicNoList = value; }
        }

        private string _connString;
        /// <summary>数据库连接串</summary>
        public string ConnString
        {
            get { return _connString; }
            set { _connString = value; }
        }

        private string _Service_AccessToken_Url;
        /// <summary>获取公众号及AccessToken的WebService的URL</summary>
        public string Service_AccessToken_Url
        {
            get { return _Service_AccessToken_Url; }
            set { _Service_AccessToken_Url = value; }
        }
        private string _PrivateKey;
        /// <summary>通讯加密私钥</summary>
        public string PrivateKey
        {
            get { return _PrivateKey; }
            set { _PrivateKey = value; }
        }

        private DBConnection dbc;
        /// <summary>服务主线程对象</summary>
        private Thread thread;
        /// <summary>
        /// 服务主线程方法
        /// </summary>
        private void ThreadMethod()
        {
            const string logHead = "[WeixinMsgFileScaner.ThreadMethod] ";
            _isRunning = true;
            while (_isRunning)
            {
                try
                {
                    Directory.GetFiles(_pathScan, _filterScan);
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
                            Cow("Error", string.Format("{0}读取文件[{1}]异常。异常信息：\r\n{2}堆栈：{3}", logHead, fullName, err.Message, err.StackTrace));
                            continue;
                        }
                        try
                        {
                            var obj = DeSerializeXml(txt, typeof(UpMsg), new UpMsg());
                            if (obj == null)
                                throw new Exception("反序列化上行消息请求包失败。");
                            var modMsg = (UpMsg)obj;

                            var sql = @"
IF NOT EXISTS (SELECT ID FROM [openid] WHERE [openid].[openid]=@OpenId And [ServId]=@ServId)
BEGIN
	INSERT INTO [OpenId]([OpenId],[ServId],[CreateTime],[State],[StateTime],[Subscribe])VALUES(@OpenId,@ServId,GETDATE(),'',GETDATE(),'1')
END ";
                            var hash = new Hashtable {{"OpenId", modMsg.FromUserName}, {"ServId", modMsg.ToUserName}};
                            dbc.ExecuteSQL(sql, hash);

                            switch (modMsg.MsgType.ToLower())
                            {
                                case "event": // 事件推送
                                    switch (modMsg.Event.ToLower())
                                    {
                                        case "subscribe": // 关注 / 扫描带参数二维码(用户未关注时)
                                        case "unsubscribe": // 取消关注
                                            var subscribeState = modMsg.Event.ToLower() == "subscribe" ? "1" : "0";
                                            sql = string.Format("UPDATE OpenId SET Subscribe='{0}', StateTime=GETDATE() WHERE OpenId='{1}' AND ServId='{2}'",
                                                subscribeState, modMsg.FromUserName, modMsg.ToUserName);
                                            dbc.ExecuteSQL(sql);

                                            if (modMsg.Event.ToLower() == "subscribe") // 关注时的推送
                                            {
                                                Cow("Debug", string.Format("{0}[{1}]关注了[{2}]", logHead, modMsg.FromUserName, modMsg.ToUserName));
                                                var sMod = MatchKeyWord(modMsg.Content, modMsg.ToUserName, true);
                                                if (sMod != null)
                                                {
                                                    //Cow("Debug", string.Format("{0}匹配到了关注欢迎信息。", logHead));
                                                    var currPn = GetPublicNoByServId(modMsg.ToUserName);
                                                    var access_token = GetAccessToken("ServId", currPn.ServId);
                                                    SendKfMsg(access_token, modMsg.FromUserName, sMod.ResponseText);
                                                }
                                            }
                                            else
                                            {
                                                Cow("Debug", string.Format("{0}[{1}]取消关注了[{2}]", logHead, modMsg.FromUserName, modMsg.ToUserName));
                                            }
                                            break;
                                        case "scan": // 扫描带参数二维码(用户已关注时)
                                            var sceneId = Convert.ToInt32(modMsg.EventKey);
                                            if (sceneId >= 20000 && sceneId < 30000)// 临时二维码的场景sceneId介于20000与30000之间时，为后台管理员扫码登录请求
                                            {
                                                sql = string.Format("UPDATE [QrLoginSession] SET [ScanFlag]='1', [OpenID]='{0}' WHERE [SceneId]={1} AND [ScanFlag]='0'", modMsg.FromUserName, sceneId);
                                                dbc.ExecuteSQL(sql);
                                                Cow("Info", string.Format("{0}管理员[{1}]扫码登录。", logHead, modMsg.FromUserName));
                                            }
                                            break;
                                        case "location": // 上报地理位置

                                            break;
                                        case "click": // 点击菜单拉取消息
                                            /* 这里注释掉是因为点击菜单事件的消息没有MsgId，而表[MsgUp]的字段[MsgId]是唯一索引的
                                            sql = "INSERT INTO [MsgUp]([ServId],[OpenId],[MsgId],[CreateTime],[Content],[Source])VALUES(@ServId,@OpenId,@MsgId,GETDATE(),@Content,@Source)";
                                            hash = new Hashtable
                                            {
                                                {"ServId", modMsg.ToUserName},
                                                {"OpenId", modMsg.FromUserName},
                                                {"MsgId", ""},
                                                {"Content", modMsg.EventKey},
                                                {"Source", txt}
                                            };
                                            dbc.ExecuteSQL(sql, hash);
                                            */
                                            var kMod = MatchKeyWord(modMsg.EventKey, modMsg.ToUserName, false);
                                            if (kMod != null)
                                            {
                                                var currPn = GetPublicNoByServId(modMsg.ToUserName);
                                                var access_token = GetAccessToken("ServId", currPn.ServId);
                                                SendKfMsg(access_token, modMsg.FromUserName, kMod.ResponseText);
                                            }
                                            break;
                                        case "view": // 点击菜单跳转链接

                                            break;
                                    }

                                    break;
                                case "text": // 文本消息
                                    sql = "INSERT INTO MsgUp(ServId,OpenId,MsgId,CreateTime,Content,Source)VALUES(@ServId,@OpenId,@MsgId,@CreateTime,@Content,@Source)";
                                    var time = new DateTime(1970, 1, 1).AddSeconds(Convert.ToInt64(modMsg.CreateTime) + 28800); // 腾讯的时间戳是格林威治时间，因此要加8个小时，即28800秒
                                    hash = new Hashtable
                                    {
                                        { "ServId", modMsg.ToUserName },
                                        { "OpenId", modMsg.FromUserName },
                                        { "MsgId", modMsg.MsgId },
                                        { "CreateTime", time },
                                        { "Content", modMsg.Content },
                                        { "Source", txt }
                                    };
                                    dbc.ExecuteSQL(sql, hash);

                                    var tMod = MatchKeyWord(modMsg.Content, modMsg.ToUserName, false);
                                    if (tMod != null)
                                    {
                                        var currPn = GetPublicNoByServId(modMsg.ToUserName);
                                        var access_token = GetAccessToken("ServId", currPn.ServId);
                                        SendKfMsg(access_token, modMsg.FromUserName, tMod.ResponseText);
                                    }
                                    break;
                                case "image": // 图片消息

                                    break;
                                case "voice": // 语音消息

                                    break;
                                case "video": // 视频消息

                                    break;
                                case "location": // 地理位置消息

                                    break;
                                case "link": // 链接消息

                                    break;
                            }


                            File.Move(fullName, _pathSucc + "\\" + fileName);
                        }
                        catch (Exception err)
                        {
                            Cow("Error", string.Format("{0}文件[{1}]解析错误。异常信息：\r\n{2}堆栈：{3}", logHead, fullName, err.Message, err.StackTrace));
                            File.Move(fullName, _pathError + "\\" + fileName);// todo: ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                        }
                    }
                }
                catch (Exception err)
                {
                    Cow("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                }
                Thread.Sleep(_timeSpan);
            }
        }

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

        /// <summary>
        /// 上行消息匹配关键字
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="servId"></param>
        /// <param name="isSubscribe">是否为关注动作消息</param>
        /// <returns></returns>
        private MsgKey MatchKeyWord(string msg, string servId, bool isSubscribe)
        {
            // 如果用户消息长度超过128字节，超过了关键字的最大长度，则肯定是无法匹配的，就不需要再去查库了
            if (msg == null) msg = "";
            if (Encoding.UTF8.GetByteCount(msg) > 128)
                return null;

            var args = new Hashtable { { "ServId", servId } };
            DataSet ds;
            if (isSubscribe)
            {
                // 匹配关注，优先匹配最新的记录
                ds = dbc.GetDataset("SELECT TOP 1 * FROM MsgKey WHERE ServId=@ServId AND MatchType='S' AND ResponseMode='A' ORDER BY Key_Id DESC", args);
                if (ds == null || ds.Tables[0].Rows.Count == 0)
                    return null;
                var mod = (MsgKey)DataRowToObject(ds.Tables[0].Rows[0], typeof(MsgKey), false);
                return mod;
            }
            args.Add("KeyWord", msg);
            // 先按完全匹配，优先匹配最新的记录
            ds = dbc.GetDataset("SELECT TOP 1 * FROM MsgKey WHERE ServId=@ServId AND KeyWord=@KeyWord AND MatchType='E' AND ResponseMode='A' ORDER BY Key_Id DESC", args);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                var mod = (MsgKey) DataRowToObject(ds.Tables[0].Rows[0], typeof (MsgKey), false);
                return mod;
            }
            // 没有符合的完全匹配，再按模糊匹配，为了保证效率，只匹配该公众号的最新的1000条模糊记录
            ds = dbc.GetDataset("SELECT TOP 1000 * FROM MsgKey WHERE ServId=@ServId AND MatchType='F' AND ResponseMode='A' ORDER BY Key_Id DESC", args);
            if (ds != null && ds.Tables[0].Rows.Count > 0)
            {
                for (var i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    var key = ds.Tables[0].Rows[i]["KeyWord"].ToString();
                    if(msg.IndexOf(key, StringComparison.Ordinal)>-1)
                    {
                        var mod = (MsgKey)DataRowToObject(ds.Tables[0].Rows[i], typeof(MsgKey), false);
                        return mod;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 将模型类序列化为Xml
        /// </summary>
        /// <param name="obj">模型类实例</param>
        /// <param name="type">模型类类型</param>
        /// <returns></returns>
        private string SerializeXml(object obj, Type type)
        {
            const string logHead = "[WeixinMsgFileScaner.SerializeXml] ";
            try
            {
                var dom = new XmlDocument();
                var root = dom.CreateElement("xml");
                dom.AppendChild(root);
                var properties = type.GetProperties();
                foreach (var p in properties)
                {
                    var value = p.GetValue(obj, null);
                    // 如果参数的值为空不参与签名
                    if (value == null || string.IsNullOrEmpty(value.ToString()))
                        continue;
                    var node = dom.CreateElement(p.Name);
                    node.InnerText = value.ToString();
                    root.AppendChild(node);
                }
                return root.OuterXml;
            }
            catch (Exception err)
            {
                Cow("Info", string.Format("{0}反序列化模型类的时候发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
            }
            return "";
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
            const string logHead = "[WeixinMsgFileScaner.DeSerializeXml] ";
            try
            {
                var dom = new XmlDocument();
                dom.LoadXml(xml);
                var root = dom.SelectSingleNode("//xml");
                if (root == null)
                {
                    Cow("Info", string.Format("{0}反序列化模型类的时候发生异常。传入的Xml中未发现[//xml]节点。", logHead));
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
                Cow("Info", string.Format("{0}反序列化模型类的时候发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
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





        private void SendKfMsg(string accessToken, string openId, string content)
        {
            var url = string.Format("https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token={0}", accessToken);

            var msgT = new RequestKfMsg_text
            {
                touser = openId,
                msgtype = "text",
                text = new RequestKfMsg_text.TextMessage { content = content }
            };
            try
            {
                var jsonRequest = JsonConvert.SerializeObject(msgT); // 序列化RequestKfMsg_text类实例为Json
                var jsonResponse = HttpPost(url, jsonRequest);
            }
            catch (Exception err)
            {
                Log("Error", string.Format("[WeixinMsgFileScaner.SendKfMsg] 发生异常。异常信息：\r\n{0}堆栈：{1}", err.Message, err.StackTrace));
            }
        }

        /// <summary>
        /// 向服务端发起GET请求，取得返回信息
        /// </summary>
        /// <param name="url">请求的URL</param>
        /// <returns></returns>
        private string HttpGet(string url)
        {
            Stream res = null;
            HttpWebResponse rsp = null;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.ContentType = "application/x-www-form-urlencoded";

                rsp = (HttpWebResponse)req.GetResponse();
                res = rsp.GetResponseStream();
                byte[] buff = new byte[4096];

                if (res != null)
                {
                    StringBuilder sb = new StringBuilder();
                    int readLen = res.Read(buff, 0, buff.Length);
                    while (readLen > 0)
                    {
                        sb.Append(Encoding.UTF8.GetString(buff, 0, readLen));
                        readLen = res.Read(buff, 0, buff.Length);
                    }
                    Log("Debug", string.Format("[WeixinMsgFileScaner.HttpGet] 向URL地址[{0}]发起GET请求，返回信息：{1}", url, sb));
                    return sb.ToString();
                }
            }
            catch (Exception err)
            {
                Log("Error", string.Format("[WeixinMsgFileScaner.HttpGet] 发生异常。异常信息：{0}\r\n堆栈：{1}", err.Message, err.StackTrace));
            }
            finally
            {
                if (res != null) res.Dispose();
                if (rsp != null) rsp.Close();
            }
            return "";
        }
        /// <summary>
        /// 向服务端发起Post请求，取得返回信息
        /// </summary>
        /// <param name="url">请求的URL</param>
        /// <param name="data">Post的数据</param>
        /// <returns></returns>
        private string HttpPost(string url, string data)
        {
            Stream res = null;
            HttpWebResponse rsp = null;
            Stream sw = null;
            StreamWriter myWriter = null;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";

                sw = req.GetRequestStream();
                myWriter = new StreamWriter(sw);
                myWriter.Write(data);
                myWriter.Close();
                myWriter.Dispose();
                myWriter = null;

                rsp = (HttpWebResponse)req.GetResponse();
                res = rsp.GetResponseStream();
                byte[] buff = new byte[4096];

                if (res != null)
                {
                    StringBuilder sb = new StringBuilder();
                    int readLen = res.Read(buff, 0, buff.Length);
                    while (readLen > 0)
                    {
                        sb.Append(Encoding.UTF8.GetString(buff, 0, readLen));
                        readLen = res.Read(buff, 0, buff.Length);
                    }
                    Log("Debug", string.Format("[WeixinMsgFileScaner.HttpPost] 向URL地址[{0}]发起Post请求，返回信息：{1}", url, sb));
                    return sb.ToString();
                }
            }
            catch (Exception err)
            {
                Log("Error", string.Format("[WeixinMsgFileScaner.HttpPost] 发生异常。异常信息：{0}\r\n堆栈：{1}", err.Message, err.StackTrace));
            }
            finally
            {
                if (res != null) res.Dispose();
                if (rsp != null) rsp.Close();
                if (sw != null) sw.Dispose();
                if (myWriter != null) myWriter.Dispose();
            }
            return "";
        }

        /// <summary>
        /// 将数据行反序列化为模型类
        /// </summary>
        /// <param name="dr">数据行对象</param>
        /// <param name="type">模型类的类型</param>
        /// <param name="needIso2Gb">文本串是否需要转码(因为Informix需要要将iso8859-1编码转换成gb18030编码)</param>
        /// <returns></returns>
        private static object DataRowToObject(DataRow dr, Type type, bool needIso2Gb)
        {
            var properties = type.GetProperties();
            var cols = dr.Table.Columns;
            var obj = type.Assembly.CreateInstance(type.FullName);
            foreach (var p in properties)
            {
                var name = p.Name;
                if (!cols.Contains(name) || dr[name] == null || dr[name] == DBNull.Value)
                    continue;
                var vStr = dr[name].ToString();
                switch (p.PropertyType.Name)
                {
                    case "Int32":
                        int vInt;
                        int.TryParse(vStr, out vInt);
                        p.SetValue(obj, vInt, null);
                        break;
                    case "Single":
                        float vSingle;
                        float.TryParse(vStr, out vSingle);
                        p.SetValue(obj, vSingle, null);
                        break;
                    case "DateTime":
                        DateTime vDt;
                        DateTime.TryParse(vStr, out vDt);
                        p.SetValue(obj, vDt, null);
                        break;
                    //case "String":
                    default:
                        p.SetValue(obj, needIso2Gb ? Iso2Gb(vStr) : vStr, null);
                        break;
                }
            }
            return obj;
        }
        /// <summary>
        /// 将iso-8859-1编码的字符串转换成gb18030编码的字符串
        /// </summary>
        /// <param name="strIso">iso-8859-1编码的字符串</param>
        /// <returns>gb18030编码的字符串</returns>
        public static string Iso2Gb(string strIso)
        {
            if (string.IsNullOrEmpty(strIso))
                return "";
            byte[] bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(strIso);
            return Encoding.GetEncoding("gb18030").GetString(bytes);
        }
        /// <summary>
        /// 根据ServID查找符合条件的公众号资料
        /// </summary>
        /// <param name="servId">指定的ServID</param>
        /// <returns></returns>
        private PublicNo GetPublicNoByServId(string servId)
        {
            return _PublicNoList.FirstOrDefault(publicNo => publicNo.ServId == servId);
        }
        /// <summary>
        /// 获取指定公众号的AccessToken
        /// </summary>
        /// <param name="argType">ServId/AppId/Token/AreaNo</param>
        /// <param name="arg">参数</param>
        /// <returns></returns>
        private string GetAccessToken(string argType, string arg)
        {
            var rnd = Guid.NewGuid().ToString().Replace("-", "");
            var sign = GetMd5(argType + arg + rnd + PrivateKey, false, "utf-8");
            var accessToken = WsAccessToken.GetAccessToken(argType, arg, rnd, sign);
            if (string.IsNullOrEmpty(accessToken))
            {
                Cow("Warn", string.Format("[Config.GetAccessToken] 警告，通过WebService方法[GetAccessToken]获取AccessToken失败。[argType={0}][arg={1}]", argType, arg));
                return "";
            }
            return accessToken;
        }

        /*
        /// <summary>
        /// 根据指定的公众号Token获取access_token
        /// </summary>
        /// <param name="publicNo">公众号PublicNo</param>
        private ResponseAccessToken GetAccessToken(PublicNo publicNo)
        {
            const string logHead = "[Config.GetAccessToken(PublicNo:publicNo)] ";

            var url = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", publicNo.AppId, publicNo.AppSecret);
            var sRes = HttpGet(url);
            if (string.IsNullOrEmpty(sRes))
            {
                Log("Info", string.Format("{0}尝试获取[access_token]异常。未能收到服务端响应。", logHead));
                return null;
            }

            Log("Info", string.Format("{0}尝试获取[access_token]完毕。收到JSON：{1}", logHead, sRes));
            try
            {
                ResponseAccessToken responseAccessToken = (ResponseAccessToken)JsonConvert.DeserializeObject(sRes, typeof(ResponseAccessToken));
                // 尝试获取[access_token]失败
                if (responseAccessToken.errcode != 0)
                {
                    Log("Info", string.Format("{0}尝试获取[access_token]失败。[errcode：{1}][errmsg：{2}]", logHead, responseAccessToken.errcode, responseAccessToken.errmsg));
                    responseAccessToken.expires_in = 0;
                    responseAccessToken.access_token = "";
                }
                else // 尝试获取[access_token]成功
                {
                    Log("Info", string.Format("{0}尝试获取[access_token]成功。[access_token：{1}][expires_in：{2}]", logHead, responseAccessToken.access_token, responseAccessToken.expires_in));
                    responseAccessToken.errcode = 0;
                    responseAccessToken.errmsg = "";
                    return responseAccessToken;
                }
            }
            catch (Exception err)
            {
                Log("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
            }
            return null;
        }
        */
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
            const string logHead = "[WeixinMsgFileScaner.GetMd5] ";
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
