using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("HCXT.Lib.BusinessService.GdLottery")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("HCXT.Lib.BusinessService.GdLottery")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。  如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("b9edab8f-6025-4f84-b261-dfa0780ba09e")]

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
[assembly: AssemblyVersion("1.0.0.4")]
[assembly: AssemblyFileVersion("1.2015.0728.1212")]
// 更新历史：
// 
// 2015-07-28
// 版 本 号：1.0.0.4
// 文件版本：1.2015.0728.1212
// 更新内容：将处理类[LotteryListenOpt]和[VerificationListenOpt]中的[DBConnection]声明提到try代码块外，将注销[OnLog]事件的代码加到finally代码块中。
//           主服务类[GdLottery]中增加一个定时清理内存的线程[_threadGc]，默认600秒回收一次内存垃圾。服务启动时启动，服务停止时停止。
// 
// 2015-07-09
// 版 本 号：1.0.0.3
// 文件版本：1.2015.0709.1814
// 更新内容：增加短信模版节点，并可直接发优惠券出货短信。
//           领取一张抽奖码[/HcAPI/GetLotteryCode.ashx]增加2个参数[bindUser][bindIp]
//           更新了兑奖、重发的逻辑
//           将兑奖动作[action=0]代码抽出来成为一个方法[action_0]
//           增加了默认批次配置[DefaultBatch]
//           修改了写短信表时中文未转码的bug
//           修改了兑奖时验证md5的传入参数
// 
// 2015-07-08
// 版 本 号：1.0.0.2
// 文件版本：1.2015.0708.1951
// 更新内容：增加兑奖侧(对外)接口[/HcAPI/GetLotteryCode.ashx]
// 
// 2015-07-07
// 版 本 号：1.0.0.1
// 文件版本：1.2015.0707.1109
// 更新内容：第一个版本的程序。已具备领取抽奖码接口[/HcAPI/GetLotteryCode.ashx]