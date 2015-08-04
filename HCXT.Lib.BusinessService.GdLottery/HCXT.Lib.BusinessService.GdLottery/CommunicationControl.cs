using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HCXT.Lib.BusinessService.GdLottery
{
    /// <summary>
    /// 消息发送器
    /// </summary>
    public class CommunicationControl
    {
        /// <summary>超时时间，默认60秒</summary>
        public int TimeOut = 60000;
        private string _res;
        private bool _retFlag;
        private readonly Encoding _enc;
        private readonly GdLottery _caller;
        /// <summary>
        /// 构造方法
        /// </summary>
        public CommunicationControl(GdLottery caller)
        {
            _res = null;
            _retFlag = false;
            _enc = Encoding.GetEncoding("gb18030");
            _caller = caller;
        }

        /// <summary>
        /// 向核心程序发送请求
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string GetMessage(string message)
        {
            const string logHead = "[CommunicationControl.GetMessage] ";
            var serverIp = _caller.EProxy_Ip;
            var serverPort = Convert.ToInt32(_caller.EProxy_Port);

            var cli = new TcpClient();
            NetworkStream stream = null;
            try
            {
                cli.Connect(serverIp, serverPort);
                if (!cli.Connected)
                    throw new Exception("连接失败");
                var eTime = DateTime.Now.AddMilliseconds(TimeOut);
                stream = cli.GetStream();
                var bytes = _enc.GetBytes(message);
                stream.Write(bytes, 0, bytes.Length);

                var thread = new Thread(ThreadReceived) { IsBackground = true };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start(stream);
                while (!_retFlag && DateTime.Now < eTime)
                {
                    Thread.Sleep(100);
                }
                // 如果是超时未返回，则强杀线程
                if (!_retFlag)
                    try
                    {
                        thread.Abort();
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch { }
                // ReSharper restore EmptyGeneralCatchClause
            }
            catch (Exception err)
            {
                _caller.Log("Error", string.Format("{0}发送消息发生异常。消息内容：[{1}]异常信息：{2}\r\n堆栈：{3}", logHead, message, err.Message, err.StackTrace));
            }
            finally
            {
                if (stream != null) stream.Close();
                cli.Close();
            }
            return _res ?? "";
        }

        private void ThreadReceived(object streamObj)
        {
            const string logHead = "[CommunicationControl.ThreadReceived] ";
            if (streamObj == null)
                return;
            var stream = (NetworkStream)streamObj;
            try
            {
                var sb = new StringBuilder();
                var buff = new byte[65536];
                do
                {
                    var len = stream.Read(buff, 0, buff.Length);
                    if (len <= 0) continue;
                    var s = _enc.GetString(buff, 0, len);
                    sb.Append(s);
                } while (stream.DataAvailable);
                _res = sb.ToString();
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch (Exception err)
            // ReSharper restore EmptyGeneralCatchClause
            {
                _caller.Log("Error", string.Format("{0}收取TCP数据流时发生异常。异常信息：{1}\r\n堆栈：{1}", logHead, err.Message));
            }
            //System.IO.File.AppendAllText(@"d:\xxxx.txt", _res);
            _retFlag = true;
        }

    }
}
