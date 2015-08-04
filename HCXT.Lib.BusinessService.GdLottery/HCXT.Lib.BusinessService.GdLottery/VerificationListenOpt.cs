using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;
using HCXT.Lib.DB.DBAccess;
// ReSharper disable InconsistentNaming
// ReSharper disable PossibleNullReferenceException

namespace HCXT.Lib.BusinessService.GdLottery
{
    /// <summary>
    /// 抽奖码库服务监听相关操作类
    /// </summary>
    class VerificationListenOpt
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="context">HTTP请求上下文对象</param>
        /// <param name="caller"></param>
        public VerificationListenOpt(HttpListenerContext context, GdLottery caller)
        {
            _caller = caller;
            var thread = new Thread(OptThreadMethod) { IsBackground = true, Priority = ThreadPriority.Normal };
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start(context);
        }

        private readonly GdLottery _caller;
        /// <summary>回调页面访问次数</summary>
        private long _accessCount;

        private void OptThreadMethod(object contextObject)
        {
            // 日志头
            const string logHead = "[VerificationListenOpt.OptThreadMethod] ";

            string resultCode;
            var dbc = new DBConnection { ConnectionType = _caller.ConnType, ConnectionString = _caller.ConnString };
            dbc.OnLog += _caller.Log;
            HttpListenerContext context = (HttpListenerContext)contextObject;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            Interlocked.Increment(ref _accessCount);
            try
            {
                var remoteIp = request.RemoteEndPoint == null ? "" : request.RemoteEndPoint.Address.ToString();
                _caller.Log("Info", string.Format("{0}收到来自[{1}]的请求。[{2}]", logHead, remoteIp, request.Url));


                //request.RawUrl

                // IP白名单验证
                if (!_caller.CheckIp(remoteIp))
                {
                    _caller.Log("Info", string.Format("{0}来自[{1}]的请求由于不在白名单上而被拒绝。", logHead, remoteIp));
                    // IP鉴权失败
                    resultCode = "Illegal IP"; // 非法的Ip访问
                    _caller.ResponsePage(resultCode, response);
                    return;
                }

                var arrUrl = new[] {"/HcAPI/GetLotteryCode.ashx"}; // 兑奖接口页面
                var absolutePath = context.Request.Url.AbsolutePath;
                var checkUrl = arrUrl.Any(s => s.ToLower() == absolutePath.ToLower());

                // Http请求URL地址验证
                if (!checkUrl)
                {
                    _caller.Log("Info", string.Format("{0}来自[{1}]的请求地址不正确。[{2}]", logHead, remoteIp, absolutePath));
                    // 系统错误
                    resultCode = "Illegal URL access"; // 非法的访问
                    _caller.ResponsePage(resultCode, response);
                    return;
                }
                // 创建码库批次 [/HcAPI/CreateBatch.ashx]
                if (absolutePath.ToLower() == arrUrl[0].ToLower())
                {
                    //参数：batch，count，sign
                    var args = new[]
                    {
                        "action", "batch", "lottery_code", "verification_code", "mobile", "factory", "area_code",
                        "serv_type", "denomination", "discount", "timestamp", "sign"
                    };
                    var hash = _caller.GetRequestArgs(args, request);
                    if (hash == null)
                    {
                        _caller.Log("Info", string.Format("{0}来自[{1}]的请求[{2}]参数不正确。", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // 非法的参数
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var action = hash["action"].ToString();
                    var batch = hash["batch"].ToString().Replace("'", "");
                    var lottery_code = hash["lottery_code"].ToString().Replace("'", "");
                    var verification_code = hash["verification_code"].ToString().Replace("'", "");
                    var mobile = hash["mobile"].ToString().Replace("'", "");
                    var factory = hash["factory"].ToString().Replace("'", "");
                    var area_code = hash["area_code"].ToString().Replace("'", "");
                    var serv_type = hash["serv_type"].ToString().Replace("'", "");
                    var denomination = hash["denomination"].ToString().Replace("'", "");
                    var discount = hash["discount"].ToString().Replace("'", "");
                    var timestamp = hash["timestamp"].ToString();
                    var sign = hash["sign"].ToString();
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    if (action == "" || batch == "" || lottery_code == "" || verification_code == "" || mobile == "" ||
                        factory == "" || area_code == "" || serv_type == "" || denomination == "" || discount == "" ||
                        timestamp == "" || sign == "")
                    {
                        _caller.Log("Info",
                            string.Format(
                                "{0}来自[{1}]的请求[{2}]参数校验失败。传入的参数[action][batch][lottery_code][verification_code][mobile][factory][area_code][serv_type][denomination][discount][timestamp][sign]不应为空。",
                                logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // 非法的参数
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var sign1 = GdLottery.GetMd5(
                        string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}", action, batch, lottery_code,
                            verification_code,
                            mobile, factory, area_code, serv_type, denomination, discount, timestamp,
                            _caller.PrivateKey), false, "utf-8").ToLower();
                    if (sign != sign1)
                    {
                        _caller.Log("Info",
                            string.Format("{0}来自[{1}]的请求[{2}]参数校验失败。传入的sign[{3}]；计算签名应为[{4}]", logHead, remoteIp,
                                absolutePath, sign, sign1));
                        resultCode = "Illegal sign"; // 非法的签名
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }

                    var sql = string.Format(@"SELECT * FROM lottery WHERE lottery_code='{0}' AND batch='{1}'",
                        lottery_code, batch);
                    var ds = dbc.GetDataset(sql);
                    if (ds.Tables[0].Rows.Count == 0)
                    {
                        resultCode = "Illegal lottery_code"; // 非法的抽奖码（在该批次的码库中匹配不到该抽奖码）
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var v_order_sn = ds.Tables[0].Rows[0]["v_order_sn"].ToString(); // 优惠券订单号
                    var v_time = ds.Tables[0].Rows[0]["v_time"].ToString(); // 兑奖时间

                    switch (action) // 动作
                    {
                        case "0": // 0兑奖
                            // 兑奖
                            action_0(response, dbc, batch, v_order_sn, mobile, lottery_code, verification_code);

                            #region 代码注释掉，已迁移到方法[action_0]中

                            /*
                        {
                            if (!string.IsNullOrEmpty(v_order_sn))
                            {
                                resultCode = "已兑过奖了"; // 
                                _caller.ResponsePage(resultCode, response);
                                return;
                            }
                            //sql = string.Format(@"UPDATE lottery SET  WHERE lottery_code='{0}' AND batch='{1}'", lottery_code, batch);

                            var sn = DateTime.Now.ToString("yyMMddHHmmssfff");
                            var msg = string.Format(@"<Proxy>
	<RequestSn>{0}</RequestSn>
	<Service code=""req.giving.card"" from=""web"">
		<MobilePhone>{1}</MobilePhone>
		<LotteryYards>{2}</LotteryYards>
		<ExpiryDateCode>{3}</ExpiryDateCode>
	</Service>
</Proxy>", sn, mobile, lottery_code, verification_code);
                            _caller.Log("Debug",string.Format("{0}向核心[{1}:{2}]发包：{3}", logHead, _caller.EProxy_Ip, _caller.EProxy_Port, msg));
                            var comm = new CommunicationControl(_caller);
                            var res = comm.GetMessage(msg);
                            if (string.IsNullOrEmpty(res))
                                throw new Exception("调用核心失败。未收到核心返回消息。");
                            _caller.Log("Debug", string.Format("{0}收到核心返回包：{1}", logHead, res));
//<Proxy>
//    <RequestSn>1427080175151</RequestSn>
//    <Service code="req.giving.card">
//        <Result>0</Result>
//        <ResultRemark></ResultRemark>
//        <OrderNumber></OrderNumber>
//    </Service>
//</Proxy> 
                            string orderNumber;// 优惠券订单号
                            try
                            {
                                var dom = new XmlDocument();
                                dom.LoadXml(res);
                                var resultVal = dom.SelectSingleNode("//Proxy/Service/Result").InnerText; // 调用核心的返回码
                                orderNumber = dom.SelectSingleNode("//Proxy/Service/OrderNumber").InnerText; // 调用核心的返回码
                                if (resultVal != "1")
                                {
                                    resultCode = string.Format("Error [{0}]", resultVal); // 
                                    _caller.ResponsePage(resultCode, response);
                                    return;
                                }
                            }
                            catch (Exception err)
                            {
                                _caller.Log("Error", string.Format("{0}解析核心返回串发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                                resultCode = "Error [action 0]"; // 
                                _caller.ResponsePage(resultCode, response);
                                return;
                            }
                            // 调用结果更新到表[lottery]中
                            sql = string.Format("UPDATE lottery SET v_code='{0}', v_order_sn='{1}', v_mobile='{2}', v_time='{3}' WHERE lottery_code='{4}' AND batch='{5}'",
                                    verification_code, orderNumber, mobile, now, lottery_code, batch);
                            dbc.ExecuteSQL(sql);
                            _caller.ResponsePage("succ", response);
                            return;
                        }
                            */

                            #endregion

                            return;

                        case "1": // 1重发
                        {
                            sql =
                                string.Format(
                                    "SELECT order_id,sn,payment_state,product_state,sessionid,ext1 FROM e_order WHERE sn='{0}'",
                                    v_order_sn);
                            var dsOrder = dbc.GetDataset(sql);
                            var order_id = dsOrder.Tables[0].Rows[0]["order_id"].ToString();
                            var ext1 = dsOrder.Tables[0].Rows[0]["ext1"].ToString();
                            var product_state = dsOrder.Tables[0].Rows[0]["product_state"].ToString();

                            if (!string.IsNullOrEmpty(v_order_sn)) // todo: 如果在抽奖码表中已有优惠券订单号，直接发短信
                            {
                                _caller.Log("Debug",
                                    string.Format("{0}在抽奖码表中已有优惠券订单号[{1}]，直接发短信给[{2}]", logHead, v_order_sn, mobile));
                                SendSms(order_id, v_order_sn, mobile);
                                resultCode = "succ"; // 
                                _caller.ResponsePage(resultCode, response);
                                return;
                            }
                            // 判断订单表[e_order]记录中是否已有[ext1]
                            if (string.IsNullOrEmpty(ext1)) // todo：如果[ext1]为空，兑奖
                            {
                                // 兑奖
                                action_0(response, dbc, batch, v_order_sn, mobile, lottery_code, verification_code);
                            }
                            else
                            {
                                if (product_state == "2") // todo: 如果[product_state]等于2，发短信
                                {
                                    SendSms(order_id, v_order_sn, mobile);
                                    resultCode = "succ"; // 
                                    _caller.ResponsePage(resultCode, response);
                                    return;
                                }
                                else // todo: 如果[product_state]不等于2，重发指令
                                {
                                    var sn = DateTime.Now.ToString("yyMMddHHmmssfff");
                                    var msg = string.Format(@"<Proxy>
    <RequestSn>{0}</RequestSn>
    <Service code=""req.recharge.card"" from=""web"">
        <OrderNumber>{1}</OrderNumber>
    </Service>
</Proxy>", sn, v_order_sn);
                                    _caller.Log("Debug",
                                        string.Format("{0}向核心[{1}:{2}]发包：{3}", logHead, _caller.EProxy_Ip,
                                            _caller.EProxy_Port, msg));
                                    var comm = new CommunicationControl(_caller);
                                    var res = comm.GetMessage(msg);
                                    if (string.IsNullOrEmpty(res))
                                        throw new Exception("调用核心失败。未收到核心返回消息。");
                                    _caller.Log("Debug", string.Format("{0}收到核心返回包：{1}", logHead, res));
                                    //<Proxy>
                                    //    <RequestSn>1427080175151</RequestSn>
                                    //    <Service code="req.recharge.card">
                                    //        <Result>0</Result>
                                    //        <ResultRemark></ResultRemark>
                                    //    </Service>
                                    //</Proxy>
                                    try
                                    {
                                        var dom = new XmlDocument();
                                        dom.LoadXml(res);
                                        var resultVal = dom.SelectSingleNode("//Proxy/Service/Result").InnerText;
                                            // 调用核心的返回码
                                        if (resultVal != "1")
                                        {
                                            resultCode = string.Format("Error [{0}]", resultVal); // 
                                            _caller.ResponsePage(resultCode, response);
                                            return;
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        _caller.Log("Error",
                                            string.Format("{0}解析核心返回串发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message,
                                                err.StackTrace));
                                        resultCode = "Error [action 1]"; // 
                                        _caller.ResponsePage(resultCode, response);
                                        return;
                                    }
                                    _caller.ResponsePage("succ", response);
                                    return;
                                }
                            }
                        }
                            return;

                        case "2": // 2确认
                        {
                            // 表中还没有优惠券订单号，返回尚未兑奖
                            if (string.IsNullOrEmpty(v_order_sn))
                            {
                                resultCode = "Not yet"; // 尚未兑奖
                                _caller.ResponsePage(resultCode, response);
                                return;
                            }
                            // 表中已有优惠券订单号，返回成功
                            _caller.ResponsePage("succ", response);
                            return;
                        }

                        default:
                            resultCode = "Illegal action"; // 非法的动作
                            _caller.ResponsePage(resultCode, response);
                            return;
                    }
                }
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                resultCode = "Error"; // 
                _caller.ResponsePage(resultCode, response);
                return;
            }
            finally
            {
                dbc.OnLog -= _caller.Log;
            }
            _caller.ResponsePage("Illegal access", response);
        }

        private void action_0(HttpListenerResponse response, DBConnection dbc, string batch, string v_order_sn, string mobile, string lottery_code, string verification_code)
        {
            const string logHead = "[VerificationListenOpt.action_0] ";
            string resultCode, sql;
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (!string.IsNullOrEmpty(v_order_sn))
            {
                _caller.Log("Debug", string.Format("{0}抽奖码[{1}]在表中有对应的订单号[{2}]。该抽奖码已兑过奖了。", logHead, lottery_code, v_order_sn));
                resultCode = "已兑过奖了"; // 
                _caller.ResponsePage(resultCode, response);
                return;
            }
            _caller.Log("Debug", string.Format("{0}抽奖码[{1}]在表中未找到对应的订单号，也就是还没抽奖。", logHead, lottery_code));
            sql = string.Format("UPDATE lottery SET v_time='{0}' WHERE lottery_code='{1}' AND batch='{2}'",
                    now, lottery_code, batch);
            dbc.ExecuteSQL(sql);

            var sn = DateTime.Now.ToString("yyMMddHHmmssfff");
            var msg = string.Format(@"<Proxy>
	<RequestSn>{0}</RequestSn>
	<Service code=""req.giving.card"" from=""web"">
		<MobilePhone>{1}</MobilePhone>
		<LotteryYards>{2}</LotteryYards>
		<ExpiryDateCode>{3}</ExpiryDateCode>
	</Service>
</Proxy>", sn, mobile, lottery_code, verification_code);
            _caller.Log("Debug", string.Format("{0}向核心[{1}:{2}]发包：{3}", logHead, _caller.EProxy_Ip, _caller.EProxy_Port, msg));
            var comm = new CommunicationControl(_caller);
            var res = comm.GetMessage(msg);
            if (string.IsNullOrEmpty(res))
                throw new Exception("调用核心失败。未收到核心返回消息。");
            _caller.Log("Debug", string.Format("{0}收到核心返回包：{1}", logHead, res));
            //<Proxy>
            //    <RequestSn>1427080175151</RequestSn>
            //    <Service code="req.giving.card">
            //        <Result>0</Result>
            //        <ResultRemark></ResultRemark>
            //        <OrderNumber></OrderNumber>
            //    </Service>
            //</Proxy> 
            string orderNumber;// 优惠券订单号
            try
            {
                var dom = new XmlDocument();
                dom.LoadXml(res);
                var resultVal = dom.SelectSingleNode("//Proxy/Service/Result").InnerText; // 调用核心的返回码
                orderNumber = dom.SelectSingleNode("//Proxy/Service/OrderNumber").InnerText; // 调用核心的返回码
                if (resultVal != "1")
                {
                    resultCode = string.Format("Error [{0}]", resultVal); // 
                    _caller.ResponsePage(resultCode, response);
                    return;
                }
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}解析核心返回串发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                resultCode = "Error [action 0]"; // 
                _caller.ResponsePage(resultCode, response);
                return;
            }
            // 调用结果更新到表[lottery]中
            sql = string.Format("UPDATE lottery SET v_code='{0}', v_order_sn='{1}', v_mobile='{2}', v_time='{3}' WHERE lottery_code='{4}' AND batch='{5}'",
                    verification_code, orderNumber, mobile, now, lottery_code, batch);
            dbc.ExecuteSQL(sql);
            _caller.ResponsePage("succ", response);
        }

        private void SendSms(string orderId, string orderSn, string mobile)
        {
            const string logHead = "[VerificationListenOpt.SendSms] ";
            try
            {
                orderSn = orderSn.Replace("'", "");
                mobile = mobile.Replace("'", "");
                var dbc = new DBConnection { ConnectionType = _caller.ConnType, ConnectionString = _caller.ConnString };
                dbc.OnLog += _caller.Log;
                var sql = string.Format("SELECT c_C_Num,c_C_Pwd,c_C_Value,c_useendtime FROM c_card_info WHERE c_C_OrderNum='{0}'", orderSn);
                var ds = dbc.GetDataset(sql);
                if (ds.Tables[0].Rows.Count == 0)
                {
                    _caller.Log("Debug", string.Format("{0}在优惠卡表[c_card_info]中未发现订单号[c_C_OrderNum={1}]的记录。", logHead, orderSn));
                    return;
                }
                var c_C_Num = ds.Tables[0].Rows[0]["c_C_Num"].ToString();
                var c_C_Pwd = ds.Tables[0].Rows[0]["c_C_Pwd"].ToString();
                var c_C_Value = ds.Tables[0].Rows[0]["c_C_Value"].ToString();
                var c_useendtime = ((DateTime)ds.Tables[0].Rows[0]["c_useendtime"]).ToString("yyyy年MM月dd日");
                var msg = string.Format(
                        "<SMS><ServType>8</ServType><ProductState>2</ProductState><OrderNumber>{0}</OrderNumber><PayMoney>{1}</PayMoney><CreateTime>{2}</CreateTime><AccNbr>{3}</AccNbr><Content>{4}</Content></SMS>",
                        orderSn, c_C_Value, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), mobile, _caller.SmsTemplate);
                msg = msg.Replace("$c_C_Num$", c_C_Num);
                msg = msg.Replace("$c_C_Pwd$", c_C_Pwd);
                msg = msg.Replace("$c_useendtime$", c_useendtime);

                var msgCode = string.Format("EC{0}{1}", DateTime.Now.ToString("yyyyMMdd"), _caller.GetRandNumeric(10));
                var hash = new Hashtable { { "sms_content", _caller.Gb2Iso(msg) } };
                sql = string.Format(@"
INSERT INTO e_sms(order_id,sms_busisoucre,sms_msgcode,sms_mobilephone,sms_content,sms_status,sms_createtime,email,email_status,sms_source) VALUES(
{0},'EC','{1}','{2}',?,0,'{3}','',0,0
)", orderId, msgCode, mobile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                dbc.ExecuteSQL(sql, hash);
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
            }
        }
    }
}