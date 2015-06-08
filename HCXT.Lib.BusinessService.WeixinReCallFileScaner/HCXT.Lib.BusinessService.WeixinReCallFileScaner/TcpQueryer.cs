using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HCXT.Lib.BusinessService.WeixinReCallFileScaner
{
    // ReSharper disable ConvertToAutoProperty
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// TCP查询器对象
    /// </summary>
    public class TcpQueryer : ITcpQueryer
    {
        #region 实现接口ITcpQueryer的成员
        private int _TimeOut = 30000;
        private string _ServerIP;
        private int _ServerPort;
        private int _BufferLength = 0x10000;
        private string _EncodingType = "gb18030";

        /// <summary>
        /// 超时时长（单位：毫秒）
        /// </summary>
        public int TimeOut
        {
            get { return _TimeOut; }
            set { _TimeOut = value; }
        }

        /// <summary>
        /// 远程服务端IP地址
        /// </summary>
        public string ServerIP
        {
            get { return _ServerIP; }
            set { _ServerIP = value; }
        }

        /// <summary>
        /// 远程服务端口
        /// </summary>
        public int ServerPort
        {
            get { return _ServerPort; }
            set { _ServerPort = value; }
        }

        /// <summary>
        /// 缓冲长度
        /// </summary>
        public int BufferLength
        {
            get { return _BufferLength; }
            set { _BufferLength = value; }
        }

        /// <summary>
        /// 编码类型(ascii,gb2312,utf-8,iso-8859-1)
        /// </summary>
        public string EncodingType
        {
            get { return _EncodingType; }
            set { _EncodingType = value; }
        }


        /// <summary>
        /// 执行查询，返回值为信息长度，如果返回-1则表示超时，-2/-3/-4/-5/-9表示发生异常
        /// </summary>
        /// <param name="para">传入的参数</param>
        /// <param name="resultString">传出的返回消息体</param>
        /// <returns>
        /// 大于0 返回值为信息长度
        /// -1    超时
        /// -2    建立TCP连接发生异常
        /// -3    获取用于发送和接收数据的NetworkStream发生异常
        /// -4    向NetworkStream写入数据发生异常 或 NetworkStream不可写
        /// -5    从NetworkStream中读取数据发生异常 或 NetworkStream不可读
        /// -9    查询线程方法发生其他异常
        /// </returns>
        public int Query(object para, out string resultString)
        {
            DateTime t = DateTime.Now.AddMilliseconds(TimeOut);
            CallOnLog("Info", string.Format("[TcpQueryer.Query] 开始Query方法。如果调用的线程方法[ThreadMethod]执行时间超过[{0}]，则将超时。", t.ToString("yyyy-MM-dd HH:mm:ss")));
            thread = new Thread(ThreadMethod) { IsBackground = true, Priority = ThreadPriority.Normal };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(para);

            while (!IsFinished && DateTime.Now < t)
            {
                Thread.Sleep(10);
            }
            CallOnLog("Info", string.Format("[TcpQueryer.Query] 超时计时器运行完毕。[IsFinished={0}][当前时间={1}][超时时间={2}][ResultLength={3}]。", IsFinished, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"), t.ToString("yyyy-MM-dd HH:mm:ss fff"), ResultLength));

            // 如果已经超时且仍未执行完毕，返回值为-1
            if (ResultLength == 0)
            {
                Stop();
            }

            try
            {
                thread.Abort();
            }
            catch (Exception Err)
            {
                //Err.ToString();
            }

            // 输出返回消息
            resultString = ResultString;
            return ResultLength;
        }

        /// <summary>
        /// 立即停止查询
        /// </summary>
        public void Stop()
        {
            CloseNetworkStream();
            CloseTcpClient();

            ResultLength = -1;
            ResultString = "超时";
        }

        /// <summary>
        /// 事件：记录日志
        /// </summary>
        public event DelegateOnLog OnLog;

        /// <summary>
        /// 事件：数据包已发送
        /// </summary>
        public event DelegateOnPackage OnSendPackage;

        /// <summary>
        /// 事件：收到数据包
        /// </summary>
        public event DelegateOnPackage OnReceivedPackage;
        #endregion

        /// <summary>
        /// 是否已查询完成
        /// </summary>
        private bool IsFinished = false;
        /// <summary>
        /// 返回值长度
        /// </summary>
        private int ResultLength = 0;
        /// <summary>
        /// 返回信息
        /// </summary>
        private string ResultString;
        /// <summary>
        /// TCP连接器对象
        /// </summary>
        private TcpClient tc;
        /// <summary>
        /// 网络数据流对象实例
        /// </summary>
        NetworkStream stream = null;
        /// <summary>
        /// 查询等待线程对象实例
        /// </summary>
        private Thread thread;
        /// <summary>
        /// 查询线程方法
        /// </summary>
        private void ThreadMethod(object para)
        {
            try
            {
                // 参数为字符串格式
                var msg = (string) para;
                // 数据包字符串转换成字节数组
                var ba = Encoding.GetEncoding(_EncodingType).GetBytes(msg);

                // 构造Socket连接器
                tc = new TcpClient();
                // 尝试建立连接
                try
                {
                    tc.Connect(ServerIP, ServerPort);
                    CallOnLog("Info", string.Format("[TcpQueryer.ThreadMethod] 建立TCP连接成功。[ServerIP={0}][ServerPort={1}][TcpClient.Connected={2}]",
                            ServerIP, ServerPort, tc.Connected));
                }
                catch (Exception Err)
                {
                    CallOnLog("Error", string.Format("[TcpQueryer.ThreadMethod] 建立TCP连接发生异常。[ServerIP={0}][ServerPort={1}]异常信息：{2}\r\n堆栈：{3}",
                            ServerIP, ServerPort, Err.Message, Err.StackTrace));
                    ResultString = "建立TCP连接发生异常";
                    ResultLength = -2;
                    IsFinished = true;
                    return;
                }

                // 如果TcpClient 实例连接成功
                if (ResultLength == 0 && tc.Connected)
                {
                    try
                    {
                        stream = tc.GetStream();
                        CallOnLog("Info", string.Format("[TcpQueryer.ThreadMethod] 获取用于发送和接收数据的NetworkStream成功。[ServerIP={0}][ServerPort={1}]",
                                ServerIP, ServerPort));
                    }
                    catch (Exception Err)
                    {
                        CallOnLog("Error", string.Format("[TcpQueryer.ThreadMethod] 获取用于发送和接收数据的NetworkStream发生异常。[ServerIP={0}][ServerPort={1}]异常信息：{2}\r\n堆栈：{3}",
                                ServerIP, ServerPort, Err.Message, Err.StackTrace));
                        ResultString = " 获取用于发送和接收数据的NetworkStream发生异常";
                        ResultLength = -3;
                        IsFinished = true;
                        return;
                    }

                    if (stream != null && ResultLength == 0)
                    {
                        CallOnLog("Info", string.Format("[TcpQueryer.ThreadMethod] 准备向NetworkStream发送数据包。[ServerIP={0}][ServerPort={1}][NetworkStream.CanWrite={2}]",
                                ServerIP, ServerPort, stream.CanWrite));
                        if (stream.CanWrite)
                        {
                            try
                            {
                                // 发送数据包
                                stream.Write(ba, 0, ba.Length);
                                CallOnLog("Info", string.Format("[TcpQueryer.ThreadMethod] 向NetworkStream发送长度为[{2}]的数据包成功。[ServerIP={0}][ServerPort={1}]",
                                        ServerIP, ServerPort, ba.Length));
                                // 触发数据包已发送事件[OnSendPackage]
                                if (OnSendPackage != null)
                                {
                                    OnSendPackage(msg);
                                }
                            }
                            catch (Exception Err)
                            {
                                CallOnLog("Error", string.Format("[TcpQueryer.ThreadMethod] 向NetworkStream写入数据发生异常。[ServerIP={0}][ServerPort={1}]异常信息：{2}\r\n堆栈：{3}",
                                        ServerIP, ServerPort, Err.Message, Err.StackTrace));
                                ResultString = "向NetworkStream写入数据发生异常";
                                ResultLength = -4;
                                IsFinished = true;
                                return;
                            }
                        }
                        else
                        {
                            CallOnLog("Error", string.Format("[TcpQueryer.ThreadMethod] NetworkStream不可写。[ServerIP={0}][ServerPort={1}]",
                                    ServerIP, ServerPort));
                            ResultString = "NetworkStream不可写";
                            ResultLength = -4;
                            IsFinished = true;
                            return;
                        }

                        CallOnLog("Info", string.Format("[TcpQueryer.ThreadMethod] 准备从NetworkStream读取数据包。[ServerIP={0}][ServerPort={1}][NetworkStream.CanRead={2}]",
                                ServerIP, ServerPort, stream.CanRead));
                        if (stream.CanRead)
                        {
                            ba = new Byte[BufferLength];
                            try
                            {
                                // 同步读取Socket返回数据包[阻塞线程]
                                int bytes = stream.Read(ba, 0, ba.Length);
                                CallOnLog("Info", string.Format("[TcpQueryer.ThreadMethod] 从NetworkStream读取到长度为[{2}]的数据包成功。[ServerIP={0}][ServerPort={1}]",
                                        ServerIP, ServerPort, bytes));
                                // 将数据包转成字符串
                                string responseData = Encoding.GetEncoding(_EncodingType).GetString(ba, 0, bytes);
                                // 触发收到数据包事件[OnReceivedPackage]
                                if (OnReceivedPackage != null)
                                {
                                    OnReceivedPackage(responseData);
                                }

                                CallOnLog("Info", string.Format("[TcpQueryer.ThreadMethod] 收到反馈消息：{0}", responseData));
                                ResultLength = bytes;
                                ResultString = responseData;
                            }
                            catch (Exception Err)
                            {
                                CallOnLog("Error", string.Format("[TcpQueryer.ThreadMethod] 从NetworkStream中读取数据发生异常。[ServerIP={0}][ServerPort={1}]异常信息：{2}\r\n堆栈：{3}",
                                        ServerIP, ServerPort, Err.Message, Err.StackTrace));
                                ResultString = "从NetworkStream中读取数据发生异常";
                                ResultLength = -5;
                                IsFinished = true;
                            }
                        }
                        else
                        {
                            CallOnLog("Error", string.Format("[TcpQueryer.ThreadMethod] NetworkStream不可读。[ServerIP={0}][ServerPort={1}]",
                                    ServerIP, ServerPort));
                            ResultString = "NetworkStream不可读";
                            ResultLength = -5;
                            IsFinished = true;
                        }
                    }
                }
                else
                {
                    ResultLength = -1;
                    IsFinished = true;
                }
            }
            catch (Exception Err)
            {
                CallOnLog("Error", string.Format("[TcpQueryer.ThreadMethod] 查询线程方法发生异常。[ServerIP={0}][ServerPort={1}]异常信息：{2}\r\n堆栈：{3}",
                        ServerIP, ServerPort, Err.Message, Err.StackTrace));
                ResultLength = -9;
            }
            finally
            {
                // 尝试关闭并释放NetworkStream
                CloseNetworkStream();
                // 尝试释放此 TcpClient 实例
                CloseTcpClient();
                IsFinished = true;
            }
        }


        /// <summary>
        /// 触发记录日志事件
        /// </summary>
        /// <param name="logType">日志类型 (Info,Debug,Warn,Error,Fatal)</param>
        /// <param name="logMsg">日志消息</param>
        private void CallOnLog(string logType, string logMsg)
        {
            if (OnLog != null)
                OnLog(logType, logMsg);
        }

        /// <summary>
        /// 尝试关闭并释放NetworkStream
        /// </summary>
        private void CloseNetworkStream()
        {
            if (stream != null)
            {
                // 尝试关闭NetworkStream
                try
                {
                    stream.Close();
                    CallOnLog("Info", "[TcpQueryer.CloseNetworkStream] 关闭(Close) NetworkStream成功。");
                }
                catch (Exception ErrClose)
                {
                    ErrClose.ToString();
                }
                // 尝试释放NetworkStream
                try
                {
                    stream.Dispose();
                    CallOnLog("Info", "[TcpQueryer.CloseNetworkStream] 释放(Dispose) NetworkStream成功。");
                }
                catch (Exception ErrClose)
                {
                    ErrClose.ToString();
                }
            }
        }
        /// <summary>
        /// 尝试释放此 TcpClient 实例
        /// </summary>
        private void CloseTcpClient()
        {
            if (tc != null && tc.Connected)
            {
                try
                {
                    tc.Close();
                    CallOnLog("Info", "[TcpQueryer.CloseTcpClient] 关闭(Close) TcpClient成功。");
                }
                catch (Exception ErrClose)
                {
                    ErrClose.ToString();
                }
            }
        }

    }
    // ReSharper restore InconsistentNaming
    // ReSharper restore ConvertToAutoProperty
}
