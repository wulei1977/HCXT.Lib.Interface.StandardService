namespace HCXT.Lib.Interface.StandardService
{
    /// <summary>
    /// 标准业务服务接口
    /// </summary>
    public interface IBusinessService
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        string ServiceName { get; set; }
        /// <summary>
        /// 服务中文名称
        /// </summary>
        string Description { get; set; }
        /// <summary>
        /// 业务处理超时时长（毫秒）
        /// </summary>
        int TimeOut { get; set; }
        /// <summary>
        /// 扫描步长时间（毫秒）
        /// </summary>
        int TimeSpan { get; set; }
        /// <summary>
        /// 服务是否允许启用
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// 服务是否允许启用
        /// </summary>
        bool IsRunning { get; set; }
        /// <summary>
        /// 服务配置XML串
        /// </summary>
        string ServiceConfigString { get; set; }
        /// <summary>
        /// 其他参数
        /// </summary>
        object OtherArgs { get; set; }
        /// <summary>
        /// 启动服务
        /// </summary>
        void Start();
        /// <summary>
        /// 停止服务
        /// </summary>
        void Stop();

        /// <summary>
        /// 业务回调事件
        /// </summary>
        event DelegateBusinessInvoke BusinessInvoke;
    }
}
