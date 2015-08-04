using System;
using System.Linq;
using System.Net;
using System.Threading;
using HCXT.Lib.DB.DBAccess;
// ReSharper disable InconsistentNaming

namespace HCXT.Lib.BusinessService.GdLottery
{
    /// <summary>
    /// �齱�����������ز�����
    /// </summary>
    class LotteryListenOpt
    {
        /// <summary>
        /// ���췽��
        /// </summary>
        /// <param name="context">HTTP���������Ķ���</param>
        /// <param name="caller"></param>
        public LotteryListenOpt(HttpListenerContext context, GdLottery caller)
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
            const string logHead = "[LotteryListenOpt.OptThreadMethod] ";

            string resultCode;
            HttpListenerContext context = (HttpListenerContext)contextObject;
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            Interlocked.Increment(ref _accessCount);
            var dbc = new DBConnection { ConnectionType = _caller.ConnType, ConnectionString = _caller.ConnString };
            dbc.OnLog += _caller.Log;
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


                var arrUrl = new[]
                {
                    "/HcAPI/CreateBatch.ashx", // �����������
                    "/HcAPI/GetLotteryCode.ashx", // ��ȡһ�ų齱��
                    "/HcAPI/DownloadBatch.ashx" // ����ָ�����εĳ齱��⵼���ļ�
                };
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

                #region ����������� [/HcAPI/CreateBatch.ashx] �����εĹ��ܷ�֧

                /*
                // ����������� [/HcAPI/CreateBatch.ashx]
                if (absolutePath.ToLower() == arrUrl[0].ToLower())
                {
                    //������batch��count��sign
                    var args = new[] { "batch", "count", "sign" };
                    var hash = _caller.GetRequestArgs(args, request);
                    if (hash == null)
                    {
                        _caller.Log("Info", string.Format("{0}����[{1}]������[{2}]��������ȷ��", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // �Ƿ��Ĳ���
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var batch = hash["batch"].ToString();
                    var count = hash["count"].ToString();
                    var sign = hash["sign"].ToString();
                    if (batch == "" || count == "" || sign == "")
                    {
                        _caller.Log("Info", string.Format("{0}����[{1}]������[{2}]����У��ʧ�ܡ�����Ĳ���[batch][count][sign]��ӦΪ�ա�", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // �Ƿ��Ĳ���
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var sign1 = GdLottery.GetMd5(string.Format("{0}{1}{2}", batch, count, _caller.PrivateKey), false, "utf-8").ToLower();
                    if (sign != sign1)
                    {
                        _caller.Log("Info", string.Format("{0}����[{1}]������[{2}]����У��ʧ�ܡ������sign[{3}]������ǩ��ӦΪ[{4}]", logHead, remoteIp, absolutePath, sign, sign1));
                        resultCode = "Illegal sign"; // �Ƿ���ǩ��
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }

                    //var dbc = new DBConnection { ConnectionType = _caller.ConnType, ConnectionString = _caller.ConnString };
                    //dbc.OnLog += _caller.Log;
                    //                    var sql = string.Format(@"INSERT INTO lottery (batch,lottery_code,creater,create_time) 
                    //SELECT FIRST 100 '{0}' batch, batch || lottery_code lottery_code,'Admin' creater,current create_time FROM lottery_base_pool ORDER BY id;",
                    //                        batch);
                    // todo�� -----------------------------------------------

                    _caller.ResponsePage("not yet", response);
                    return;
                }
                */

                #endregion

                // todo: ��ȡһ�ų齱�� [/HcAPI/GetLotteryCode.ashx] --------------------------------------------------------------------
                if (absolutePath.ToLower() == arrUrl[1].ToLower())
                {
                    var args = new[] {"orderSource", "orderSn", "orderMobile", "bindUser", "bindIp", "attach", "sign"};
                    var hash = _caller.GetRequestArgs(args, request);
                    if (hash == null)
                    {
                        _caller.Log("Info", string.Format("{0}����[{1}]������[{2}]��������ȷ��", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // �Ƿ��ķ���
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var orderSource = hash["orderSource"].ToString().Replace("'", "");
                    var orderSn = hash["orderSn"].ToString().Replace("'", "");
                    var orderMobile = hash["orderMobile"].ToString().Replace("'", ""); //����Ϊ��
                    var bindUser = hash["bindUser"].ToString().Replace("'", "");
                    var bindIp = hash["bindIp"].ToString().Replace("'", "");
                    var attach = hash["attach"].ToString();
                    var sign = hash["sign"].ToString().ToLower();
                    if (orderSource == "" || orderSn == "" || sign == "")
                    {
                        _caller.Log("Info",
                            string.Format("{0}����[{1}]������[{2}]����У��ʧ�ܡ�����Ĳ���[orderSource][orderSn][sign]��ӦΪ�ա�", logHead,
                                remoteIp, absolutePath));
                        resultCode = "Illegal args"; // �Ƿ��Ĳ���
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var sign1 =
                        GdLottery.GetMd5(
                            string.Format("{0}{1}{2}{3}{4}{5}{6}", orderSource, orderSn, orderMobile, bindUser, bindIp,
                                attach, _caller.PrivateKey), false, "utf-8").ToLower();
                    if (sign != sign1)
                    {
                        _caller.Log("Info",
                            string.Format("{0}����[{1}]������[{2}]����У��ʧ�ܡ������sign[{3}]������ǩ��ӦΪ[{4}]", logHead, remoteIp,
                                absolutePath, sign, sign1));
                        resultCode = "Illegal sign"; // �Ƿ���ǩ��
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }

                    var ds =
                        dbc.GetDataset(
                            string.Format(
                                "SELECT a.*, b.ext8 FROM lottery a LEFT JOIN lottery_batch b ON b.batch=a.batch WHERE order_sn='{0}'",
                                orderSn.Replace("'", "")));
                    // ����Ķ������Ѿ����ù��齱���ˣ�ֱ���ҵ�������¼����������
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        var dr = ds.Tables[0].Rows[0];
                        // todo: ���أ� batch,lottery_code,order_source,order_sn,order_mobile,bind_time,ext8
                        resultCode = string.Format("{0},{1},{2},{3},{4},{5},{6}", dr["batch"], dr["lottery_code"],
                            dr["order_source"], dr["order_sn"], dr["order_mobile"], dr["bind_time"],
                            _caller.Iso2Gb(dr["ext8"].ToString()).Replace(",", "��")); // ��Ƕ����滻��ȫ��
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    else // ����Ķ�������δ���ù��齱��
                    {
                        //var sql = string.Format("SELECT FIRST 1 * FROM lottery WHERE batch='{0}' AND order_sn IS NULL ORDER BY lottery_id", batch);
                        // todo: ������ʱ��û�п��ǲ������⣬�������������ѡ����ԣ�����������������������������������������������������������������������
                        //var sql = "SELECT FIRST 1 a.*, b.ext8 FROM lottery a LEFT JOIN lottery_batch b ON b.batch=a.batch WHERE order_sn IS NULL ORDER BY lottery_id";
                        // todo: ����Ĭ������
                        var sql =
                            string.Format(
                                "SELECT FIRST 1 a.*, b.ext8 FROM lottery a LEFT JOIN lottery_batch b ON b.batch=a.batch WHERE a.batch='{0}' AND order_sn IS NULL ORDER BY lottery_id",
                                _caller.DefaultBatch);
                        ds = dbc.GetDataset(sql);
                        if (ds.Tables[0].Rows.Count == 0)
                        {
                            resultCode = "No available data"; // û�п��õ�����
                            _caller.ResponsePage(resultCode, response);
                            return;
                        }
                        else // ƥ�䵽һ�����г齱���¼
                        {
                            var batch = ds.Tables[0].Rows[0]["batch"].ToString();
                            var lottery_code = ds.Tables[0].Rows[0]["lottery_code"].ToString();
                            var lottery_id = ds.Tables[0].Rows[0]["lottery_id"].ToString();
                            var ext8 = _caller.Iso2Gb(ds.Tables[0].Rows[0]["ext8"].ToString()).Replace(",", "��");
                                // ��Ƕ����滻��ȫ��
                            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            sql =
                                string.Format(
                                    "UPDATE lottery SET order_source='{0}', order_sn='{1}', order_mobile='{2}', bind_time='{3}', bind_user='{4}', bind_ip='{5}' WHERE lottery_id={6}",
                                    orderSource, orderSn, orderMobile, now, bindUser, bindIp, lottery_id);
                            dbc.ExecuteSQL(sql);
                            // todo: ���أ� batch,lottery_code,order_source,order_sn,order_mobile,bind_time,ext8
                            resultCode = string.Format("{0},{1},{2},{3},{4},{5},{6}", batch, lottery_code, orderSource,
                                orderSn, orderMobile, now, ext8);
                            _caller.ResponsePage(resultCode, response);
                            return;
                        }
                    }
                }
                // ����ָ�����εĳ齱��⵼���ļ� [/HcAPI/DownloadBatch.ashx]
                if (absolutePath.ToLower() == arrUrl[2].ToLower())
                {
                    _caller.ResponsePage("this interface can't to use", response);
                    return;
                }
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}�����쳣���쳣��Ϣ��{1}\r\n��ջ��{2}", logHead, err.Message, err.StackTrace));
                resultCode = "Error"; // �Ƿ���ǩ��
                _caller.ResponsePage(resultCode, response);
                return;
            }
            finally
            {
                dbc.OnLog -= _caller.Log;
            }
            _caller.ResponsePage("Illegal access", response);
        }
    }
}