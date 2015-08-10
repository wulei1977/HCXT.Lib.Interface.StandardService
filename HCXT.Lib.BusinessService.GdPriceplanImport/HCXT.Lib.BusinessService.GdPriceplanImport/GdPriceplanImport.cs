using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using HCXT.Lib.BusinessService.GdPriceplanImport.Model;
using HCXT.Lib.BusinessService.GdPriceplanImport.Ws_Priceplan;
using HCXT.Lib.DB.DBAccess;
using HCXT.Lib.Interface.StandardService;

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToAutoProperty

namespace HCXT.Lib.BusinessService.GdPriceplanImport
{
    /// <summary>
    /// 广东电渠导入价格计划服务
    /// </summary>
    public class GdPriceplanImport: IBusinessService
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
            const string logHead = "[GdPriceplanImport.Start] ";
            _thread = new Thread(ThreadMethod) { IsBackground = true };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();

            _threadGc = new Thread(GcThreadMethod) { IsBackground = true };
            _threadGc.SetApartmentState(ApartmentState.STA);
            _threadGc.Start();

            Cow("Info", string.Format("{0}服务[{1}]主线程已启动。", logHead, _serviceName));
        }
        /// <summary>停止服务</summary>
        public void Stop()
        {
            const string logHead = "[GdPriceplanImport.Stop] ";
            _isRunning = false;
            Cow("Info", string.Format("{0}服务线程正在停止，请稍候。", logHead));
            try
            {
                _thread.Abort();
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
                const string logHead = "[GdPriceplanImport.ServiceConfigString.set] ";
                #region 传入的服务配置XML串格式示例
                /*
<root>
  <HCXT.Lib.BusinessService.GdPriceplanImport>
    <!--服务名称-->
    <ServiceName>HCXT.Lib.BusinessService.GdPriceplanImport</ServiceName>
    <!--服务中文名称-->
    <Description>广东电渠导入价格计划服务</Description>
    <!--服务是否允许启用-->
    <Enabled>true</Enabled>
    <!--扫描步长时间（毫秒）-->
    <TimeSpan>2000</TimeSpan>
    <!--数据库连接类型(MSSQL)-->
    <ConnType>informix</ConnType>
    <!--数据库连接串-->
    <ConnString>Database=db_tyzf;Host=192.168.2.14;Server=ol_tyzf;Service=1526;Protocol=onsoctcp;UID=informix;Password=informix;enlist=true</ConnString>
    <!--通讯加密私钥-->
    <PrivateKey>1234567890123456</PrivateKey>
    <!--WebService地址URL-->
    <Ws_PriceImport_Url>http://xxx.xxx.xxx.xxx/priceimport.asmx</Ws_PriceImport_Url>
    <!--WebService请求的参数配置：请求方系统-->
    <WsArg_callerSystem>9902</WsArg_callerSystem>
    <!--WebService请求的参数配置：省份ID-->
    <WsArg_provinceId>44</WsArg_provinceId>
    <!--WebService请求的参数配置：业务类型 2固话，1宽带  华泰目前都是使用1 部分2或 1-->
    <WsArg_businessType>1</WsArg_businessType>
    <!--WebService请求的参数配置：业务代码:获取价格计划编码 Q022-->
    <WsArg_businessCode>Q022</WsArg_businessCode>
    <!--WebService请求的参数配置：版本-->
    <WsArg_version>1.00</WsArg_version>
    <!--默认的到期时间-->
    <Default_ExpDate>2100-01-01 00:00:00</Default_ExpDate>
    <!--建立临时表SQL模版-->
    <Sql_Create>
      create table temp_priceimport  (
      id                   SERIAL                          not null,
      batch_id             INTEGER                         not null,
      import_time          DATETIME YEAR TO SECOND,
      priceplan_id         INTEGER                         not null,
      comp_id              VARCHAR(10)                     not null,
      priceplan_name       VARCHAR(250)                    not null,
      service_id           INTEGER                         not null,
      service_name         VARCHAR(80)                     not null,
      servtype_id          VARCHAR(10),
      delay_type           VARCHAR(3)                      not null,
      delay_value          INTEGER                         not null,
      price                INTEGER                         not null,
      prodtype_id          VARCHAR(6)                      not null,
      expdate              DATETIME YEAR TO SECOND,
      import_state         INTEGER,
      ext1                 VARCHAR(250),
      ext2                 VARCHAR(250),
      primary key (id) constraint PK_temp_PRICE
      )
    </Sql_Create>
    <!--删除临时表SQL模版-->
    <Sql_Drop>
      drop table temp_priceimport
    </Sql_Drop>
    <!--从临时表插入正式表的SQL模版-->
    <Sql_Import>
      INSERT INTO e_priceplan_import(
      ,,,,,,,,,,,,,,,,
      )SELECT
      ,,,,,,,,,,,,,,,,
      FROM temp_priceimport
    </Sql_Import>
  </HCXT.Lib.BusinessService.GdPriceplanImport>
</root>
                 */
                #endregion
                try
                {
                    var dom = new XmlDocument();
                    dom.LoadXml(value);
                    const string sRoot = "//HCXT.Lib.BusinessService.GdPriceplanImport/";
                    var arrName = new[]
                    {
                        "ServiceName", "Description", "Enabled", "TimeSpan", "ConnType", "ConnString", "PrivateKey",
                        "Ws_PriceImport_Url","EnableSavePriceplanFile", "WsArg_callerSystem", "WsArg_provinceId", "WsArg_businessType",
                        "WsArg_businessCode", "WsArg_version","DeleteOldBatch_Offset", "Default_ExpDate", "ScanMode",
                        "Sql_Create", "Sql_Drop", "Sql_Import", "Sql_GetBatchId"
                    };
                    foreach (var pName in arrName)
                    {
                        var node = dom.SelectSingleNode(sRoot + pName);
                        if (node == null || string.IsNullOrWhiteSpace(node.InnerText))
                            throw new Exception(string.Format("配置文件节点[{0}]未取到值！", pName));
                        var p = typeof(GdPriceplanImport).GetProperty(pName);
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
                            case "DateTime":
                                DateTime vDate;
                                DateTime.TryParse(node.InnerText, out vDate);
                                p.SetValue(this, vDate, null);
                                break;
                            default:
                                p.SetValue(this, node.InnerText, null);
                                break;
                        }
                    }

                    // 初始化按指定时间点扫描配置
                    var xnl = dom.SelectNodes(string.Format("{0}TimePointList/TimePoint", sRoot));
                    if (xnl == null)
                        throw new Exception("配置文件中未发现[TimePointList/TimePoint]节点");
                    TimePointList = new List<string>();
                    foreach (XmlNode xn in xnl)
                        TimePointList.Add(xn.InnerText);
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

        #region 自定义公共属性
        /// <summary>连接类型</summary>
        public string ConnType { get; set; }
        /// <summary>数据库连接串</summary>
        public string ConnString { get; set; }
        /// <summary>通讯加密私钥</summary>
        public string PrivateKey { get; set; }
        /// <summary>WebService地址URL</summary>
        public string Ws_PriceImport_Url { get; set; }
        /// <summary>是否将每次通过接口取得的数据存为文件</summary>
        public bool EnableSavePriceplanFile { get; set; }
        /// <summary>WebService请求的参数配置：请求方系统</summary>
        public string WsArg_callerSystem { get; set; }
        /// <summary>WebService请求的参数配置：省份ID</summary>
        public string WsArg_provinceId { get; set; }
        /// <summary>WebService请求的参数配置：业务类型 2固话，1宽带  华泰目前都是使用1 部分2或 1</summary>
        public string WsArg_businessType { get; set; }
        /// <summary>WebService请求的参数配置：业务代码:获取价格计划编码 Q022</summary>
        public string WsArg_businessCode { get; set; }
        /// <summary>WebService请求的参数配置：版本</summary>
        public string WsArg_version { get; set; }
        /// <summary>删除旧的导入批次数据时的条件偏移量(例如：10表示删除所有批次号小于当前批次号减10的批次数据)</summary>
        public int DeleteOldBatch_Offset { get; set; }
        /// <summary>默认的到期时间</summary>
        public DateTime Default_ExpDate { get; set; }
        /// <summary>扫描模式(StepTimeSpan/TimePointList)</summary>
        public string ScanMode { get; set; }
        /// <summary>按指定时间点扫描的时间点列表</summary>
        public List<string> TimePointList;

        /// <summary>建立临时表SQL模版</summary>
        public string Sql_Create { get; set; }
        /// <summary>删除临时表SQL模版</summary>
        public string Sql_Drop { get; set; }
        /// <summary>从临时表插入正式表的SQL模版</summary>
        public string Sql_Import { get; set; }
        /// <summary>获取最新批次号语句（取表[e_priceplan_import]中的最大batch_id再加1</summary>
        public string Sql_GetBatchId { get; set; }
        #endregion

        #region 业务代码块
        private long _pc;
        /// <summary>服务线程对象</summary>
        private Thread _thread;
        /// <summary>
        /// 服务线程方法
        /// </summary>
        private void ThreadMethod()
        {
            // 日志头
            const string logHead = "[GdPriceplanImport.ThreadMethod] ";

            Cow("Info", string.Format("{0}服务线程开始扫描。", logHead));
            _isRunning = true;
            while (_isRunning)
            {
                var now = DateTime.Now;
                // 如果是按时间点扫描
                if (ScanMode == "TimePointList")
                {
                    // 当前时间点字符串
                    var strTime = now.ToString("HH:mm:ss");
                    // 是否在扫描时间点上
                    var AsPoint = TimePointList.Any(t => t == strTime);
                    // 如果不在扫描时间点上，则Sleep半秒钟，然后直接结束本次循环，进入下次循环
                    if (!AsPoint)
                    {
                        Thread.Sleep(501);
                        continue;
                    }
                    // 如果在扫描时间点上，则继续进行下面的扫描同步操作
                }

                _pc++;
                Cow("Info", string.Format("{0}服务线程第[{1}]轮扫描开始。", logHead, _pc));
                // 初始化数据库链接器
                var _dbc = new DBConnection { ConnectionType = ConnType, ConnectionString = ConnString };
                _dbc.OnLog += Log;
                Cow("Debug", string.Format("{0}初始化数据库链接器完成。[ConnectionType={1}][ConnectionString={2}]", logHead, ConnType, ConnString));
                // WebService初始化
                var ws = new WsBusiService { Url = Ws_PriceImport_Url };
                Cow("Debug", string.Format("{0}WebService初始化完成。[Url={1}]", logHead, ws.Url));
                try
                {
                    // 获取最新批次号（取表[e_priceplan_import]中的最大batch_id再加一）
                    var ds = _dbc.GetDataset(Sql_GetBatchId);
                    var batch_id = Convert.ToInt32(ds.Tables[0].Rows[0][0]);
                    Log("Info", string.Format("{0}获取最新批次号为[{1}]", logHead, batch_id));

                    // todo：通过WebService获取Xml数据，并分析数据
                    string md5; // 校验码
                    var requestStr = GetRequestString(out md5); // 请求串
                    Log("Info", string.Format("{0}即将调用WebService。请求参数串：[{1}]MD5串：[{2}]", logHead, requestStr, md5));
                    var responseStr = ws.busiOper(requestStr, md5); // 同步调用WebService，并得到返回值

                    // 将每次通过接口取得的数据存为文件
                    SavePriceplanFile(responseStr);

                    var dom = new XmlDocument();
                    dom.LoadXml(responseStr);

                    // 数据总数
                    var recordCount = GetRecordCount(dom);

                    // 先判断临时表是否存在，如果临时表存在，先drop掉临时表
                    if (ExistTable("temp_priceimport", _dbc))
                        _dbc.ExecuteSQL(Sql_Drop);
                    // 创建临时表
                    _dbc.ExecuteSQL(Sql_Create);

                    // todo：扫描Xml数据，得到模型列表
                    var modList = GetRecordInfoList(dom);

                    // 尝试删除旧的批次数据
                    DeleteOldBatchData(_dbc, batch_id);

                    // todo：Insert到临时表(注意批次号)
                    InsertIntoTempTable(modList, _dbc, batch_id);

                    // 统计一下临时表中插成功的数据行数
                    var recordCountTemp = GetRecordCount(batch_id, _dbc);
                    if (recordCount != recordCountTemp)
                    {
                        Cow("Info",
                            string.Format("{0}接口得到数据[{1}]条，成功插入临时表[{2}]条，数据记录数不符，本次操作失败！", logHead, recordCount,
                                recordCountTemp));
                        continue;
                    }

                    // 将临时表中的数据select插入到推荐价格计划导入表[e_priceplan_import]中
                    _dbc.ExecuteSQL(Sql_Import);

                    // drop掉临时表
                    _dbc.ExecuteSQL(Sql_Drop);
                }
                catch (Exception err)
                {
                    Cow("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                }
                finally
                {
                    _dbc.OnLog -= Log;
                    ws.Dispose();
                }

                // 如果是时间点模式，这里sleep1秒即可
                if (ScanMode == "TimePointList")
                {
                    Cow("Info", string.Format("{0}服务线程第[{1}]轮扫描完成。", logHead, _pc));
                    Thread.Sleep(1000);
                }
                else // 如果是时间步长模式，这里可以计算一下下次扫描的时间
                {
                    Cow("Info", string.Format("{0}服务线程第[{1}]轮扫描完成。下一轮扫描将在[{2}]秒后开始。", logHead, _pc, TimeSpan/1000));
                    Thread.Sleep(TimeSpan);
                }
            }
            _isRunning = false;
            Cow("Info", string.Format("{0}服务线程方法已结束", logHead));
        }

        private string GetRequestString(out string md5)
        {
            var arrName = new[] { "callerSystem", "provinceId", "businessType", "businessCode", "requestNumber", "callTime", "version" };
            var arrVal = new[]
            {
                WsArg_callerSystem, WsArg_provinceId, WsArg_businessType, WsArg_businessCode,
                DateTime.Now.ToString("yyyyMMddHHmmssfff"), DateTime.Now.ToString("yyyyMMddHHmmss"), WsArg_version
            };
            var sb = new StringBuilder();
            sb.Append("<content><head>");
            for(var i=0; i<arrName.Length; i++)
                sb.AppendFormat("<{0}>{1}</{0}>", arrName[i], arrVal[i]);  
            sb.Append("</head><body></body></content>");
            md5 = StrToMd5(string.Format("{0}{1}", sb, PrivateKey));
            var req = string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?><request>{0}</request>", sb);
            return req;
        }

        private int GetRecordCount(XmlDocument dom)
        {
            const string xPath = "//response/content/body/record/recordInfo/paramInfo/paramCode[text()='Size']/../paramValue/value";
            var xn = dom.SelectSingleNode(xPath);
            return xn == null ? -1 : Convert.ToInt32(xn.InnerText);
        }
        private int GetRecordCount(int batch_id, DBConnection dbc)
        {
            var sql = string.Format("SELECT COUNT(*) FROM temp_priceimport WHERE batch_id={0}", batch_id);
            var ds = dbc.GetDataset(sql);
            return Convert.ToInt32(ds.Tables[0].Rows[0][0]);
        }
        private List<RecordInfo> GetRecordInfoList(XmlDocument dom)
        {
            const string xPath = "//response/content/body/record/recordInfoList/recordInfo";
            const string subPathName = "paramInfo/paramCode[text()='$Name$']/..";
            const string subPathVal = "paramValue/value";
            var result = new List<RecordInfo>();
            var type = typeof (RecordInfo);
            var xnl = dom.SelectNodes(xPath);
            if (xnl == null)
                throw new Exception(string.Format("XML中找不到[{0}]节点。", xPath));
            foreach (XmlNode xn in xnl)
            {
                var mod = new RecordInfo();
                var ps = type.GetProperties();
                foreach (var p in ps)
                {
                    var n = xn.SelectSingleNode(subPathName.Replace("$Name$", p.Name));
                    var t = "";
                    if (n != null)
                    {
                        var nv = n.SelectSingleNode(subPathVal);
                        t = nv == null ? "" : nv.InnerText;
                    }
                    switch (p.PropertyType.Name)
                    {
                        case "Int32":
                            int vInt;
                            int.TryParse(t, out vInt);
                            p.SetValue(mod, vInt, null);
                            break;
                        case "DateTime":// 只有一个[ExpDate]字段是DateTime类型
                            DateTime vDate;
                            if (!DateTime.TryParse(t, out vDate))
                                vDate = Default_ExpDate; // 如果节点中没给[ExpDate]，则表示[ExpDate]无限大，即采用配置文件中的Default_ExpDate
                            p.SetValue(mod, vDate, null);
                            break;
                        default:
                            p.SetValue(mod, t, null);
                            break;
                    }
                }
                result.Add(mod);
            }
            return result;
        }
        private void DeleteOldBatchData(DBConnection dbc, int batch_id)
        {
            // 如果偏移量参数小于1，则不予处理
            if (DeleteOldBatch_Offset < 1) return;
            var sql = string.Format("DELETE FROM e_priceplan_import WHERE batch_id<{0}", batch_id - DeleteOldBatch_Offset);
            dbc.ExecuteSQL(sql);
        }
        private void InsertIntoTempTable(List<RecordInfo> list, DBConnection dbc, int batch_id)
        {
            const string timeMask = "yyyy-MM-dd HH:mm:ss";
            const string srcSql = "INSERT INTO temp_priceimport (batch_id,import_time,priceplan_id,comp_id,priceplan_name,service_id,service_name,service_name_type,servtype_id,delay_type,delay_value,price,prodtype_id,expdate,import_state,ext1,ext2)VALUES($values$)";
            foreach (var mod in list)
            {
                var sVal = batch_id // batch_id
                           + ",'" + DateTime.Now.ToString(timeMask) + "'" // import_time
                           + "," + mod.PricePlanId // priceplan_id
                           + ",'" + mod.CompId + "'" // comp_id
                           + ",'" + mod.PricePlanName + "'" // priceplan_name
                           + "," + mod.ServiceId // service_id
                           + ",'" + mod.ServiceName + "'" // service_name
                           + ",'" + mod.ServiceNameType + "'" // service_name_type
                           + ",'" + mod.ServTypeId + "'" // servtype_id
                           + ",'" + mod.DelayType + "'" // delay_type
                           + "," + mod.DelayValue // delay_value
                           + "," + mod.Price // price
                           + ",'" + mod.ProdTypeId + "'" // prodtype_id
                           + ",'" + mod.ExpDate.ToString(timeMask) + "'" // expdate
                           + ",1" // import_state
                           + ",''" // ext1
                           + ",''"; // ext2
                var sql = srcSql.Replace("$values$", Gb2Iso(sVal));
                dbc.ExecuteSQL(sql);
            }
        }

        private void SavePriceplanFile(string msg)
        {
            if (!EnableSavePriceplanFile) return;
            const string dir = "PriceplanFiles";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(string.Format("{0}\\Priceplan_{1}.xml", dir, DateTime.Now.ToString("yyyyMMddHHmmss")), msg);
        }
        #endregion


        #region 工具方法
        private string StrToMd5(string paramStr)
        {
            if (string.IsNullOrEmpty(paramStr)) return "";

            var res = Encoding.GetEncoding("gbk").GetBytes(paramStr);
            var md = new MD5CryptoServiceProvider();

            var hash = md.ComputeHash(res);
            var sbuilder = new StringBuilder();
            foreach (var t in hash)
            {
                int i = t;
                if (i < 0)
                    i += 256;
                if (i < 16)
                    sbuilder.Append("0");
                sbuilder.Append(Convert.ToString(i, 16));
            }
            return sbuilder.ToString();
        }
        /// <summary>
        /// 判断Informix中一张表是否存在
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="dbc"></param>
        /// <returns></returns>
        private bool ExistTable(string tableName, DBConnection dbc)
        {
            var ds = dbc.GetDataset(string.Format("SELECT * FROM systables WHERE tabname ='{0}' AND tabtype = 'T'", tableName));
            return ds.Tables[0].Rows.Count > 0;
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
        /*
        /// <summary>
        /// 将iso-8859-1编码的字符串转换成gb2312编码的字符串
        /// </summary>
        /// <param name="strIso">iso-8859-1编码的字符串</param>
        /// <returns>gb2312编码的字符串</returns>
        private string Iso2Gb(string strIso)
        {
            if (string.IsNullOrEmpty(strIso))
                return "";
            var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(strIso);
            return Encoding.GetEncoding("gb2312").GetString(bytes);
        }
        */
        /// <summary>
        /// 将gb2312编码的字符串转换成iso-8859-1编码的字符串
        /// </summary>
        /// <param name="strGb">gb2312编码的字符串</param>
        /// <returns>iso-8859-1编码的字符串</returns>
        private string Gb2Iso(string strGb)
        {
            if (string.IsNullOrEmpty(strGb))
                return "";
            var bytes = Encoding.GetEncoding("gb2312").GetBytes(strGb);
            return Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        }
        #endregion

        #region 日志相关方法
        private readonly object lockerConsole = new object();
        /// <summary>
        /// 通过BusinessInvoke事件扔出日志
        /// </summary>
        /// <param name="logType">日志类型(Info/Debug/Warn/Error/Fatal)</param>
        /// <param name="logMsg">日志内容文本</param>
        private void Log(string logType, string logMsg)
        {
            if (BusinessInvoke != null)
                BusinessInvoke("log", logType, logMsg);
        }
        /// <summary>
        /// 在控制台输出日志，并通过BusinessInvoke事件扔出日志
        /// </summary>
        /// <param name="logType">日志类型(Info/Debug/Warn/Error/Fatal)</param>
        /// <param name="message">日志内容文本</param>
        private void Cow(string logType, string message)
        {
            Log(logType, message);
            lock (lockerConsole)
                Console.Out.WriteLine("{0} [{1}] {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), logType, message);
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
            const string logHead = "[GdPriceplanImport.GcThreadMethod] ";
            _isRunningGc = true;
            Log("Info", string.Format("{0}定时回收内存垃圾线程已启动。", logHead));
            long pc = 0;
            while (_isRunningGc)
            {
                try
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
                catch (Exception err)
                {
                    Log("Error", string.Format("{0}定时回收内存垃圾线程发生异常。异常信息：{1}", logHead, err.Message));
                }
            }
        }
        #endregion
    }
}
