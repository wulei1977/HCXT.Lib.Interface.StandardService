namespace HCXT.Lib.BusinessService.WeixinReCallFileScaner
{
    /// <summary>
    /// TCP��ѯ���ӿ�
    /// </summary>
    public interface ITcpQueryer
    {
        /// <summary>
        /// ��ʱʱ������λ�����룩
        /// </summary>
        int TimeOut { get; set; }
        /// <summary>
        /// Զ�̷����IP��ַ
        /// </summary>
        string ServerIP { get; set; }
        /// <summary>
        /// Զ�̷���˿�
        /// </summary>
        int ServerPort { get; set; }
        /// <summary>
        /// ���峤��
        /// </summary>
        int BufferLength { get; set; }
        /// <summary>
        /// ��������(ascii,gb2312,utf-8,iso-8859-1)
        /// </summary>
        string EncodingType { get; set; }
        /// <summary>
        /// ִ�в�ѯ������ֵΪ��Ϣ���ȣ��������-1���ʾ��ʱ��-2/-3/-4/-5/-9��ʾ�����쳣
        /// </summary>
        /// <param name="para">����Ĳ���</param>
        /// <param name="resultString">�����ķ�����Ϣ��</param>
        /// <returns>
        /// ����0 ����ֵΪ��Ϣ����
        /// -1    ��ʱ
        /// -2    ����TCP���ӷ����쳣
        /// -3    ��ȡ���ڷ��ͺͽ������ݵ�NetworkStream�����쳣
        /// -4    ��NetworkStreamд�����ݷ����쳣 �� NetworkStream����д
        /// -5    ��NetworkStream�ж�ȡ���ݷ����쳣 �� NetworkStream���ɶ�
        /// -9    ��ѯ�̷߳������������쳣
        /// </returns>
        int Query(object para, out string resultString);
        /// <summary>
        /// ����ֹͣ��ѯ
        /// </summary>
        void Stop();

        /// <summary>
        /// �¼�����¼��־
        /// </summary>
        event DelegateOnLog OnLog;
        /// <summary>
        /// �¼������ݰ��ѷ���
        /// </summary>
        event DelegateOnPackage OnSendPackage;
        /// <summary>
        /// �¼����յ����ݰ�
        /// </summary>
        event DelegateOnPackage OnReceivedPackage;
    }
    /// <summary>
    /// ί�У���¼��־
    /// </summary>
    /// <param name="logType">��־���� (Info,Debug,Warn,Error,Fatal)</param>
    /// <param name="logMsg">��־��Ϣ</param>
    public delegate void DelegateOnLog(string logType, string logMsg);
    /// <summary>
    /// ί�У����ݰ��շ�
    /// </summary>
    /// <param name="packageContent">���ݰ�����</param>
    public delegate void DelegateOnPackage(string packageContent);
}