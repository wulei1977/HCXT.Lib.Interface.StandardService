using System.Reflection;
//using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("HCXT.Lib.BusinessService.GdPriceplanImport")]
[assembly: AssemblyDescription("广东电渠导入价格计划服务类库")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("HCXT.Lib.BusinessService.GdPriceplanImport")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。  如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("8fe0478e-463e-46c5-8337-87332421707f")]

// 程序集的版本信息由下面四个值组成: 
//
//      主版本
//      次版本 
//      生成号
//      修订号
//
// 可以指定所有这些值，也可以使用“生成号”和“修订号”的默认值，
// 方法是按如下所示使用“*”: 
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.3")]
[assembly: AssemblyFileVersion("1.2015.0728.1140")]

// 更新历史：
// 
// 2015-07-28
// 版 本 号：1.0.0.3
// 文件版本：1.2015.0728.1140
// 更新内容：由于发现调用WebService时出现了内存溢出，因此将WebService由一次总声明循环调用改为每次调用都进行局部声明，调用完毕立即Dispose。
//           增加一个定时清理内存的线程[_threadGc]，默认600秒回收一次内存垃圾。服务启动时启动，服务停止时停止。
// 
// 2015-07-17
// 版 本 号：1.0.0.2
// 文件版本：1.2015.0717.1618
// 更新内容：插入数据库的时候，对字符串字段进行转码。
//           增加删除旧批次数据功能，以及配置文件中增加旧批次号偏移量条件[DeleteOldBatch_Offset]
//           增加按时间点扫描方式，以及相应的文件配置。[ScanMode][TimePointList/TimePoint]
// 
// 2015-07-15
// 版 本 号：1.0.0.1
// 文件版本：1.2015.0715.1752
// 更新内容：第一个版本的广东电渠导入价格计划服务类库程序。
