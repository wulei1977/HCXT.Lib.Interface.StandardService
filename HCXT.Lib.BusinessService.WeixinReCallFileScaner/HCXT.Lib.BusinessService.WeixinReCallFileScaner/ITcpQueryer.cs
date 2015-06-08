namespace HCXT.Lib.BusinessService.WeixinReCallFileScaner
{
    /// <summary>
    /// TCP查询器接口
    /// </summary>
    public interface ITcpQueryer
    {
        /// <summary>
        /// 超时时长（单位：毫秒）
        /// </summary>
        int TimeOut { get; set; }
        /// <summary>
        /// 远程服务端IP地址
        /// </summary>
        string ServerIP { get; set; }
        /// <summary>
        /// 远程服务端口
        /// </summary>
        int ServerPort { get; set; }
        /// <summary>
        /// 缓冲长度
        /// </summary>
        int BufferLength { get; set; }
        /// <summary>
        /// 编码类型(ascii,gb2312,utf-8,iso-8859-1)
        /// </summary>
        string EncodingType { get; set; }
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
        int Query(object para, out string resultString);
        /// <summary>
        /// 立即停止查询
        /// </summary>
        void Stop();

        /// <summary>
        /// 事件：记录日志
        /// </summary>
        event DelegateOnLog OnLog;
        /// <summary>
        /// 事件：数据包已发送
        /// </summary>
        event DelegateOnPackage OnSendPackage;
        /// <summary>
        /// 事件：收到数据包
        /// </summary>
        event DelegateOnPackage OnReceivedPackage;
    }
    /// <summary>
    /// 委托：记录日志
    /// </summary>
    /// <param name="logType">日志类型 (Info,Debug,Warn,Error,Fatal)</param>
    /// <param name="logMsg">日志消息</param>
    public delegate void DelegateOnLog(string logType, string logMsg);
    /// <summary>
    /// 委托：数据包收发
    /// </summary>
    /// <param name="packageContent">数据包内容</param>
    public delegate void DelegateOnPackage(string packageContent);
}