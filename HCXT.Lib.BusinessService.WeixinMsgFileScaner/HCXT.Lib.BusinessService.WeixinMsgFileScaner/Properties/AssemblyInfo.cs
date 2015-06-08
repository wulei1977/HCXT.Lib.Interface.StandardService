using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("HCXT.Lib.BusinessService.WeixinMsgFileScaner")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("HCXT.Lib.BusinessService.WeixinMsgFileScaner")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。  如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("665597e3-2835-420f-b1a5-d8739a91568c")]

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
[assembly: AssemblyVersion("1.0.0.8")]
[assembly: AssemblyFileVersion("1.2015.0605.1544")]
// 更新历史：
// 
// 2015-06-05
// 版 本 号：1.0.0.8
// 文件版本：1.2015.0605.1544
// 更新内容：修改模糊匹配算法的Bug。
// 
// 2015-06-04
// 版 本 号：1.0.0.7
// 文件版本：1.2015.0604.1708
// 更新内容：增加关注公众号的下行欢迎消息发送
// 
// 2015-06-04
// 版 本 号：1.0.0.6
// 文件版本：1.2015.0604.0124
// 更新内容：增加引入获取公众号及AccessToken的WebService[Ws_AccessToken]
//           [WeixinMsgFileScaner]类增加获取公众号及AccessToken的WebService的URL属性[Service_AccessToken_Url]
//           [WeixinMsgFileScaner]类增加通讯加密私钥属性[PrivateKey]
//           修改公众号列表属性[PublicNoList]的初始化方式，由从数据表中加载，改为通过WebService加载
//           修改公众号获取AccessToken的方法[GetAccessToken]，由自行向腾讯获取，改为通过WebService获取
// 
// 2015-05-28
// 版 本 号：1.0.0.5
// 文件版本：1.2015.0528.1455
// 更新内容：[WeixinMsgFileScaner]类增加公众号列表属性[PublicNoList]，初始化的时候从表[PublicNo]中加载
//           处理上行关键字消息和文本消息时匹配表中的关键字，匹配到的发送客服下行文本消息。
// 
// 2015-04-09
// 版 本 号：1.0.0.4
// 文件版本：1.2015.0409.1221
// 更新内容：修正一个Bug，在处理[关注/取消关注]推送消息时，更新OpenId表时误将全表所有记录的关注状态都更新了。
// 
// 2015-03-28
// 版 本 号：1.0.0.3
// 文件版本：1.2015.0328.1617
// 更新内容：增加管理员扫码登陆动作(SceneID介于2万和3万之间)的分析处理。(即更新[QrLoginSession]表的状态以及填入OpenID)
// 
// 2015-03-25
// 版 本 号：1.0.0.2
// 文件版本：1.2015.0325.2340
// 更新内容：增加对消息包的分析。
//           对新的OpenID进行入库
//           更新关注状态
//           上行文本消息入库
// 
// 2015-03-11
// 版 本 号：1.0.0.1
// 文件版本：1.2015.0311.1234
// 更新内容：第一个版本的消息处理类。功能仅包括文件处理。