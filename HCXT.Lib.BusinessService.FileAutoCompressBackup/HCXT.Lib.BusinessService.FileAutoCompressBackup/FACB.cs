using System;
using System.Diagnostics;
using System.Threading;
using HCXT.Lib.Interface.StandardService;

// ReSharper disable InconsistentNaming
namespace HCXT.Lib.BusinessService.FileAutoCompressBackup
{
    /// <summary>
    /// FileAutoCompressBackup
    /// 文件自动压缩备份服务实例类
    /// </summary>
    class FACB
    {
        /// <summary>事件，扔出日志</summary>
        public event DelegateBusinessInvoke OnCow;

        private void CallOnCow(string logType, string msg)
        {
            if (OnCow != null)
                OnCow("log", logType, msg);
        }

        /// <summary>策略名称</summary>
        public string Name = "";
        /// <summary>说明</summary>
        public string ServiceDesc = "";
        /// <summary>批处理文件</summary>
        public string ModelBatFile = "";
        /// <summary>备份时间策略 Hour/Day/Month</summary>
        public string BackupBy = "";
        /// <summary>时间策略提前量</summary>
        public string PerUnits = "";
        /// <summary>源目录</summary>
        public string SourceFolder = "";
        /// <summary>目标目录</summary>
        public string ObjectFolder = "";
        /// <summary>源文件通配符</summary>
        public string SourceFileNameMask = "";
        /// <summary>目标文件通配符</summary>
        public string ObjectFileNameMask = "";

        private string ThisTime = "";
        private string mask = "";
        private Thread thread;
        private bool IsRunning;
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            thread = new Thread(ThreadMethod) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            try
            {
                IsRunning = false;
                thread.Abort();
            }
            catch (Exception Err)
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                Err.ToString();
            }
        }
        private void ThreadMethod()
        {
            IsRunning = true;
            while (IsRunning)
            {
                string thisTime = GetThisTimeString();
                if (thisTime != ThisTime)
                {
                    ThisTime = thisTime;
                    string srcFNM = SourceFileNameMask.Replace("%BackupBy%", thisTime);
                    string objFNM = ObjectFileNameMask.Replace("%BackupBy%", thisTime);
                    CallOnCow("Info", string.Format("[{0}]{3} 开始压缩文件[{1}]到[{2}]", Name, srcFNM, objFNM, ServiceDesc));

                    Process p = new Process();
                    // -s  创建固实压缩文件
                    // -m5 压缩比5最高
                    // -y  假设对全部询问都回答是
                    // -md 以KB为单位的字典大小(64,128,256,512,1024,2048,4096 or A-G)
                    // -df 压缩后删除文件
                    p.StartInfo = new ProcessStartInfo("rar.exe", string.Format(" a -s -m5 -y -md4096 -df {0}{1} {2}{3}", ObjectFolder, objFNM, SourceFolder, srcFNM));
                    p.Start();

                    CallOnCow("Info", string.Format("[{0}]{3} 结束压缩文件[{1}]到[{2}]", Name, srcFNM, objFNM, ServiceDesc));
                }

                Thread.Sleep(1000);
            }
            IsRunning = false;
        }

        private string GetThisTimeString()
        {
            DateTime now = DateTime.Now;
            string thisTime = "";
            switch (BackupBy.Trim().ToLower())
            {
                case "hour":
                    mask = "yyyyMMddHH";
                    thisTime = now.AddHours(0 - Convert.ToInt32(PerUnits)).ToString(mask);
                    break;
                case "day":
                    mask = "yyyyMMdd";
                    thisTime = now.AddDays(0 - Convert.ToInt32(PerUnits)).ToString(mask);
                    break;
                case "month":
                    mask = "yyyyMM";
                    thisTime = now.AddMonths(0 - Convert.ToInt32(PerUnits)).ToString(mask);
                    break;
            }
            return thisTime;
        }
    }
}