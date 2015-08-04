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
    /// �齱�����������ز�����
    /// </summary>
    class VerificationListenOpt
    {
        /// <summary>
        /// ���췽��
        /// </summary>
        /// <param name="context">HTTP���������Ķ���</param>
        /// <param name="caller"></param>
        public VerificationListenOpt(HttpListenerContext context, GdLottery caller)
        {
            _caller = caller;
            var thread = new Thread(OptThreadMethod) { IsBackground = true, Priority = ThreadPriority.Normal };
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start(context);
        }

        private readonly GdLottery _caller;
        /// <summary>�ص�ҳ����ʴ���</summary>
        private long _accessCount;

        private void OptThreadMethod(object contextObject)
        {
            // ��־ͷ
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
                _caller.Log("Info", string.Format("{0}�յ�����[{1}]������[{2}]", logHead, remoteIp, request.Url));


                //request.RawUrl

                // IP��������֤
                if (!_caller.CheckIp(remoteIp))
                {
                    _caller.Log("Info", string.Format("{0}����[{1}]���������ڲ��ڰ������϶����ܾ���", logHead, remoteIp));
                    // IP��Ȩʧ��
                    resultCode = "Illegal IP"; // �Ƿ���Ip����
                    _caller.ResponsePage(resultCode, response);
                    return;
                }

                var arrUrl = new[] {"/HcAPI/GetLotteryCode.ashx"}; // �ҽ��ӿ�ҳ��
                var absolutePath = context.Request.Url.AbsolutePath;
                var checkUrl = arrUrl.Any(s => s.ToLower() == absolutePath.ToLower());

                // Http����URL��ַ��֤
                if (!checkUrl)
                {
                    _caller.Log("Info", string.Format("{0}����[{1}]�������ַ����ȷ��[{2}]", logHead, remoteIp, absolutePath));
                    // ϵͳ����
                    resultCode = "Illegal URL access"; // �Ƿ��ķ���
                    _caller.ResponsePage(resultCode, response);
                    return;
                }
                // ����������� [/HcAPI/CreateBatch.ashx]
                if (absolutePath.ToLower() == arrUrl[0].ToLower())
                {
                    //������batch��count��sign
                    var args = new[]
                    {
                        "action", "batch", "lottery_code", "verification_code", "mobile", "factory", "area_code",
                        "serv_type", "denomination", "discount", "timestamp", "sign"
                    };
                    var hash = _caller.GetRequestArgs(args, request);
                    if (hash == null)
                    {
                        _caller.Log("Info", string.Format("{0}����[{1}]������[{2}]��������ȷ��", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // �Ƿ��Ĳ���
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
                                "{0}����[{1}]������[{2}]����У��ʧ�ܡ�����Ĳ���[action][batch][lottery_code][verification_code][mobile][factory][area_code][serv_type][denomination][discount][timestamp][sign]��ӦΪ�ա�",
                                logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // �Ƿ��Ĳ���
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
                            string.Format("{0}����[{1}]������[{2}]����У��ʧ�ܡ������sign[{3}]������ǩ��ӦΪ[{4}]", logHead, remoteIp,
                                absolutePath, sign, sign1));
                        resultCode = "Illegal sign"; // �Ƿ���ǩ��
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }

                    var sql = string.Format(@"SELECT * FROM lottery WHERE lottery_code='{0}' AND batch='{1}'",
                        lottery_code, batch);
                    var ds = dbc.GetDataset(sql);
                    if (ds.Tables[0].Rows.Count == 0)
                    {
                        resultCode = "Illegal lottery_code"; // �Ƿ��ĳ齱�루�ڸ����ε������ƥ�䲻���ó齱�룩
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var v_order_sn = ds.Tables[0].Rows[0]["v_order_sn"].ToString(); // �Ż�ȯ������
                    var v_time = ds.Tables[0].Rows[0]["v_time"].ToString(); // �ҽ�ʱ��

                    switch (action) // ����
                    {
                        case "0": // 0�ҽ�
                            // �ҽ�
                            action_0(response, dbc, batch, v_order_sn, mobile, lottery_code, verification_code);

                            #region ����ע�͵�����Ǩ�Ƶ�����[action_0]��

                            /*
                        {
                            if (!string.IsNullOrEmpty(v_order_sn))
                            {
                                resultCode = "�Ѷҹ�����"; // 
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
                            _caller.Log("Debug",string.Format("{0}�����[{1}:{2}]������{3}", logHead, _caller.EProxy_Ip, _caller.EProxy_Port, msg));
                            var comm = new CommunicationControl(_caller);
                            var res = comm.GetMessage(msg);
                            if (string.IsNullOrEmpty(res))
                                throw new Exception("���ú���ʧ�ܡ�δ�յ����ķ�����Ϣ��");
                            _caller.Log("Debug", string.Format("{0}�յ����ķ��ذ���{1}", logHead, res));
//<Proxy>
//    <RequestSn>1427080175151</RequestSn>
//    <Service code="req.giving.card">
//        <Result>0</Result>
//        <ResultRemark></ResultRemark>
//        <OrderNumber></OrderNumber>
//    </Service>
//</Proxy> 
                            string orderNumber;// �Ż�ȯ������
                            try
                            {
                                var dom = new XmlDocument();
                                dom.LoadXml(res);
                                var resultVal = dom.SelectSingleNode("//Proxy/Service/Result").InnerText; // ���ú��ĵķ�����
                                orderNumber = dom.SelectSingleNode("//Proxy/Service/OrderNumber").InnerText; // ���ú��ĵķ�����
                                if (resultVal != "1")
                                {
                                    resultCode = string.Format("Error [{0}]", resultVal); // 
                                    _caller.ResponsePage(resultCode, response);
                                    return;
                                }
                            }
                            catch (Exception err)
                            {
                                _caller.Log("Error", string.Format("{0}�������ķ��ش������쳣���쳣��Ϣ��{1}\r\n��ջ��{2}", logHead, err.Message, err.StackTrace));
                                resultCode = "Error [action 0]"; // 
                                _caller.ResponsePage(resultCode, response);
                                return;
                            }
                            // ���ý�����µ���[lottery]��
                            sql = string.Format("UPDATE lottery SET v_code='{0}', v_order_sn='{1}', v_mobile='{2}', v_time='{3}' WHERE lottery_code='{4}' AND batch='{5}'",
                                    verification_code, orderNumber, mobile, now, lottery_code, batch);
                            dbc.ExecuteSQL(sql);
                            _caller.ResponsePage("succ", response);
                            return;
                        }
                            */

                            #endregion

                            return;

                        case "1": // 1�ط�
                        {
                            sql =
                                string.Format(
                                    "SELECT order_id,sn,payment_state,product_state,sessionid,ext1 FROM e_order WHERE sn='{0}'",
                                    v_order_sn);
                            var dsOrder = dbc.GetDataset(sql);
                            var order_id = dsOrder.Tables[0].Rows[0]["order_id"].ToString();
                            var ext1 = dsOrder.Tables[0].Rows[0]["ext1"].ToString();
                            var product_state = dsOrder.Tables[0].Rows[0]["product_state"].ToString();

                            if (!string.IsNullOrEmpty(v_order_sn)) // todo: ����ڳ齱����������Ż�ȯ�����ţ�ֱ�ӷ�����
                            {
                                _caller.Log("Debug",
                                    string.Format("{0}�ڳ齱����������Ż�ȯ������[{1}]��ֱ�ӷ����Ÿ�[{2}]", logHead, v_order_sn, mobile));
                                SendSms(order_id, v_order_sn, mobile);
                                resultCode = "succ"; // 
                                _caller.ResponsePage(resultCode, response);
                                return;
                            }
                            // �ж϶�����[e_order]��¼���Ƿ�����[ext1]
                            if (string.IsNullOrEmpty(ext1)) // todo�����[ext1]Ϊ�գ��ҽ�
                            {
                                // �ҽ�
                                action_0(response, dbc, batch, v_order_sn, mobile, lottery_code, verification_code);
                            }
                            else
                            {
                                if (product_state == "2") // todo: ���[product_state]����2��������
                                {
                                    SendSms(order_id, v_order_sn, mobile);
                                    resultCode = "succ"; // 
                                    _caller.ResponsePage(resultCode, response);
                                    return;
                                }
                                else // todo: ���[product_state]������2���ط�ָ��
                                {
                                    var sn = DateTime.Now.ToString("yyMMddHHmmssfff");
                                    var msg = string.Format(@"<Proxy>
    <RequestSn>{0}</RequestSn>
    <Service code=""req.recharge.card"" from=""web"">
        <OrderNumber>{1}</OrderNumber>
    </Service>
</Proxy>", sn, v_order_sn);
                                    _caller.Log("Debug",
                                        string.Format("{0}�����[{1}:{2}]������{3}", logHead, _caller.EProxy_Ip,
                                            _caller.EProxy_Port, msg));
                                    var comm = new CommunicationControl(_caller);
                                    var res = comm.GetMessage(msg);
                                    if (string.IsNullOrEmpty(res))
                                        throw new Exception("���ú���ʧ�ܡ�δ�յ����ķ�����Ϣ��");
                                    _caller.Log("Debug", string.Format("{0}�յ����ķ��ذ���{1}", logHead, res));
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
                                            // ���ú��ĵķ�����
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
                                            string.Format("{0}�������ķ��ش������쳣���쳣��Ϣ��{1}\r\n��ջ��{2}", logHead, err.Message,
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

                        case "2": // 2ȷ��
                        {
                            // ���л�û���Ż�ȯ�����ţ�������δ�ҽ�
                            if (string.IsNullOrEmpty(v_order_sn))
                            {
                                resultCode = "Not yet"; // ��δ�ҽ�
                                _caller.ResponsePage(resultCode, response);
                                return;
                            }
                            // ���������Ż�ȯ�����ţ����سɹ�
                            _caller.ResponsePage("succ", response);
                            return;
                        }

                        default:
                            resultCode = "Illegal action"; // �Ƿ��Ķ���
                            _caller.ResponsePage(resultCode, response);
                            return;
                    }
                }
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}�����쳣���쳣��Ϣ��{1}\r\n��ջ��{2}", logHead, err.Message, err.StackTrace));
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
                _caller.Log("Debug", string.Format("{0}�齱��[{1}]�ڱ����ж�Ӧ�Ķ�����[{2}]���ó齱���Ѷҹ����ˡ�", logHead, lottery_code, v_order_sn));
                resultCode = "�Ѷҹ�����"; // 
                _caller.ResponsePage(resultCode, response);
                return;
            }
            _caller.Log("Debug", string.Format("{0}�齱��[{1}]�ڱ���δ�ҵ���Ӧ�Ķ����ţ�Ҳ���ǻ�û�齱��", logHead, lottery_code));
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
            _caller.Log("Debug", string.Format("{0}�����[{1}:{2}]������{3}", logHead, _caller.EProxy_Ip, _caller.EProxy_Port, msg));
            var comm = new CommunicationControl(_caller);
            var res = comm.GetMessage(msg);
            if (string.IsNullOrEmpty(res))
                throw new Exception("���ú���ʧ�ܡ�δ�յ����ķ�����Ϣ��");
            _caller.Log("Debug", string.Format("{0}�յ����ķ��ذ���{1}", logHead, res));
            //<Proxy>
            //    <RequestSn>1427080175151</RequestSn>
            //    <Service code="req.giving.card">
            //        <Result>0</Result>
            //        <ResultRemark></ResultRemark>
            //        <OrderNumber></OrderNumber>
            //    </Service>
            //</Proxy> 
            string orderNumber;// �Ż�ȯ������
            try
            {
                var dom = new XmlDocument();
                dom.LoadXml(res);
                var resultVal = dom.SelectSingleNode("//Proxy/Service/Result").InnerText; // ���ú��ĵķ�����
                orderNumber = dom.SelectSingleNode("//Proxy/Service/OrderNumber").InnerText; // ���ú��ĵķ�����
                if (resultVal != "1")
                {
                    resultCode = string.Format("Error [{0}]", resultVal); // 
                    _caller.ResponsePage(resultCode, response);
                    return;
                }
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}�������ķ��ش������쳣���쳣��Ϣ��{1}\r\n��ջ��{2}", logHead, err.Message, err.StackTrace));
                resultCode = "Error [action 0]"; // 
                _caller.ResponsePage(resultCode, response);
                return;
            }
            // ���ý�����µ���[lottery]��
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
                    _caller.Log("Debug", string.Format("{0}���Żݿ���[c_card_info]��δ���ֶ�����[c_C_OrderNum={1}]�ļ�¼��", logHead, orderSn));
                    return;
                }
                var c_C_Num = ds.Tables[0].Rows[0]["c_C_Num"].ToString();
                var c_C_Pwd = ds.Tables[0].Rows[0]["c_C_Pwd"].ToString();
                var c_C_Value = ds.Tables[0].Rows[0]["c_C_Value"].ToString();
                var c_useendtime = ((DateTime)ds.Tables[0].Rows[0]["c_useendtime"]).ToString("yyyy��MM��dd��");
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
                _caller.Log("Error", string.Format("{0}�����쳣���쳣��Ϣ��{1}\r\n��ջ��{2}", logHead, err.Message, err.StackTrace));
            }
        }
    }
}