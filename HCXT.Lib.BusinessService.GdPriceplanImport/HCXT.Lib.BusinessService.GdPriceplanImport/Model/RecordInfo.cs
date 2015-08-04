using System;

namespace HCXT.Lib.BusinessService.GdPriceplanImport.Model
{
    /// <summary>
    /// 模型类
    /// </summary>
    [Serializable]
    public class RecordInfo
    {
        /// <summary>价格计划标识</summary>
        public int PricePlanId { get; set; }
        /// <summary>分公司标识</summary>
        public string CompId { get; set; }
        /// <summary>价格计划名称</summary>
        public string PricePlanName { get; set; }
        /// <summary>带宽标识</summary>
        public int ServiceId { get; set; }
        /// <summary>带宽名称</summary>
        public string ServiceName { get; set; }
        /// <summary>带宽分类</summary>
        public string ServiceNameType { get; set; }
        /// <summary>用户类型</summary>
        public string ServTypeId { get; set; }
        /// <summary>周期类型</summary>
        public string DelayType { get; set; }
        /// <summary>周期值</summary>
        public int DelayValue { get; set; }
        /// <summary>价格:单位：分</summary>
        public int Price { get; set; }
        /// <summary>产品分类</summary>
        public string ProdTypeId { get; set; }
        /// <summary>失效时间 为空表示无限大</summary>
        public DateTime ExpDate { get; set; }
    }
}
