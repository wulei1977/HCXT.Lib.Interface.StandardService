using System;
using System.Linq;
using System.Net;
using System.Threading;
using HCXT.Lib.DB.DBAccess;
// ReSharper disable InconsistentNaming

namespace HCXT.Lib.BusinessService.GdLottery
{
    /// <summary>
    /// 抽奖码库服务监听相关操作类
    /// </summary>
    class LotteryListenOpt
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="context">HTTP请求上下文对象</param>
        /// <param name="caller"></param>
        public LotteryListenOpt(HttpListenerContext context, GdLottery caller)
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


                var arrUrl = new[]
                {
                    "/HcAPI/CreateBatch.ashx", // 创建码库批次
                    "/HcAPI/GetLotteryCode.ashx", // 领取一张抽奖码
                    "/HcAPI/DownloadBatch.ashx" // 下载指定批次的抽奖码库导出文件
                };
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

                #region 创建码库批次 [/HcAPI/CreateBatch.ashx] 已屏蔽的功能分支

                /*
                // 创建码库批次 [/HcAPI/CreateBatch.ashx]
                if (absolutePath.ToLower() == arrUrl[0].ToLower())
                {
                    //参数：batch，count，sign
                    var args = new[] { "batch", "count", "sign" };
                    var hash = _caller.GetRequestArgs(args, request);
                    if (hash == null)
                    {
                        _caller.Log("Info", string.Format("{0}来自[{1}]的请求[{2}]参数不正确。", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // 非法的参数
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var batch = hash["batch"].ToString();
                    var count = hash["count"].ToString();
                    var sign = hash["sign"].ToString();
                    if (batch == "" || count == "" || sign == "")
                    {
                        _caller.Log("Info", string.Format("{0}来自[{1}]的请求[{2}]参数校验失败。传入的参数[batch][count][sign]不应为空。", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // 非法的参数
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var sign1 = GdLottery.GetMd5(string.Format("{0}{1}{2}", batch, count, _caller.PrivateKey), false, "utf-8").ToLower();
                    if (sign != sign1)
                    {
                        _caller.Log("Info", string.Format("{0}来自[{1}]的请求[{2}]参数校验失败。传入的sign[{3}]；计算签名应为[{4}]", logHead, remoteIp, absolutePath, sign, sign1));
                        resultCode = "Illegal sign"; // 非法的签名
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }

                    //var dbc = new DBConnection { ConnectionType = _caller.ConnType, ConnectionString = _caller.ConnString };
                    //dbc.OnLog += _caller.Log;
                    //                    var sql = string.Format(@"INSERT INTO lottery (batch,lottery_code,creater,create_time) 
                    //SELECT FIRST 100 '{0}' batch, batch || lottery_code lottery_code,'Admin' creater,current create_time FROM lottery_base_pool ORDER BY id;",
                    //                        batch);
                    // todo： -----------------------------------------------

                    _caller.ResponsePage("not yet", response);
                    return;
                }
                */

                #endregion

                // todo: 领取一张抽奖码 [/HcAPI/GetLotteryCode.ashx] --------------------------------------------------------------------
                if (absolutePath.ToLower() == arrUrl[1].ToLower())
                {
                    var args = new[] {"orderSource", "orderSn", "orderMobile", "bindUser", "bindIp", "attach", "sign"};
                    var hash = _caller.GetRequestArgs(args, request);
                    if (hash == null)
                    {
                        _caller.Log("Info", string.Format("{0}来自[{1}]的请求[{2}]参数不正确。", logHead, remoteIp, absolutePath));
                        resultCode = "Illegal args"; // 非法的访问
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    var orderSource = hash["orderSource"].ToString().Replace("'", "");
                    var orderSn = hash["orderSn"].ToString().Replace("'", "");
                    var orderMobile = hash["orderMobile"].ToString().Replace("'", ""); //可以为空
                    var bindUser = hash["bindUser"].ToString().Replace("'", "");
                    var bindIp = hash["bindIp"].ToString().Replace("'", "");
                    var attach = hash["attach"].ToString();
                    var sign = hash["sign"].ToString().ToLower();
                    if (orderSource == "" || orderSn == "" || sign == "")
                    {
                        _caller.Log("Info",
                            string.Format("{0}来自[{1}]的请求[{2}]参数校验失败。传入的参数[orderSource][orderSn][sign]不应为空。", logHead,
                                remoteIp, absolutePath));
                        resultCode = "Illegal args"; // 非法的参数
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
                            string.Format("{0}来自[{1}]的请求[{2}]参数校验失败。传入的sign[{3}]；计算签名应为[{4}]", logHead, remoteIp,
                                absolutePath, sign, sign1));
                        resultCode = "Illegal sign"; // 非法的签名
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }

                    var ds =
                        dbc.GetDataset(
                            string.Format(
                                "SELECT a.*, b.ext8 FROM lottery a LEFT JOIN lottery_batch b ON b.batch=a.batch WHERE order_sn='{0}'",
                                orderSn.Replace("'", "")));
                    // 传入的订单号已经领用过抽奖码了，直接找到这条记录返给调用者
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        var dr = ds.Tables[0].Rows[0];
                        // todo: 返回： batch,lottery_code,order_source,order_sn,order_mobile,bind_time,ext8
                        resultCode = string.Format("{0},{1},{2},{3},{4},{5},{6}", dr["batch"], dr["lottery_code"],
                            dr["order_source"], dr["order_sn"], dr["order_mobile"], dr["bind_time"],
                            _caller.Iso2Gb(dr["ext8"].ToString()).Replace(",", "，")); // 半角逗号替换成全角
                        _caller.ResponsePage(resultCode, response);
                        return;
                    }
                    else // 传入的订单号尚未领用过抽奖码
                    {
                        //var sql = string.Format("SELECT FIRST 1 * FROM lottery WHERE batch='{0}' AND order_sn IS NULL ORDER BY lottery_id", batch);
                        // todo: 这里暂时先没有考虑策略问题，后续会加入批次选择策略￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥￥
                        //var sql = "SELECT FIRST 1 a.*, b.ext8 FROM lottery a LEFT JOIN lottery_batch b ON b.batch=a.batch WHERE order_sn IS NULL ORDER BY lottery_id";
                        // todo: 先用默认批次
                        var sql =
                            string.Format(
                                "SELECT FIRST 1 a.*, b.ext8 FROM lottery a LEFT JOIN lottery_batch b ON b.batch=a.batch WHERE a.batch='{0}' AND order_sn IS NULL ORDER BY lottery_id",
                                _caller.DefaultBatch);
                        ds = dbc.GetDataset(sql);
                        if (ds.Tables[0].Rows.Count == 0)
                        {
                            resultCode = "No available data"; // 没有可用的数据
                            _caller.ResponsePage(resultCode, response);
                            return;
                        }
                        else // 匹配到一条空闲抽奖码记录
                        {
                            var batch = ds.Tables[0].Rows[0]["batch"].ToString();
                            var lottery_code = ds.Tables[0].Rows[0]["lottery_code"].ToString();
                            var lottery_id = ds.Tables[0].Rows[0]["lottery_id"].ToString();
                            var ext8 = _caller.Iso2Gb(ds.Tables[0].Rows[0]["ext8"].ToString()).Replace(",", "，");
                                // 半角逗号替换成全角
                            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            sql =
                                string.Format(
                                    "UPDATE lottery SET order_source='{0}', order_sn='{1}', order_mobile='{2}', bind_time='{3}', bind_user='{4}', bind_ip='{5}' WHERE lottery_id={6}",
                                    orderSource, orderSn, orderMobile, now, bindUser, bindIp, lottery_id);
                            dbc.ExecuteSQL(sql);
                            // todo: 返回： batch,lottery_code,order_source,order_sn,order_mobile,bind_time,ext8
                            resultCode = string.Format("{0},{1},{2},{3},{4},{5},{6}", batch, lottery_code, orderSource,
                                orderSn, orderMobile, now, ext8);
                            _caller.ResponsePage(resultCode, response);
                            return;
                        }
                    }
                }
                // 下载指定批次的抽奖码库导出文件 [/HcAPI/DownloadBatch.ashx]
                if (absolutePath.ToLower() == arrUrl[2].ToLower())
                {
                    _caller.ResponsePage("this interface can't to use", response);
                    return;
                }
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}发生异常。异常信息：{1}\r\n堆栈：{2}", logHead, err.Message, err.StackTrace));
                resultCode = "Error"; // 非法的签名
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