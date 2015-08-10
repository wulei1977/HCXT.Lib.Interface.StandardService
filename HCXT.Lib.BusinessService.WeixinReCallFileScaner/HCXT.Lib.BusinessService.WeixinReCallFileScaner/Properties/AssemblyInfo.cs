using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// 有关程序集的常规信息通过以下
// 特性集控制。更改这些特性值可修改
// 与程序集关联的信息。
[assembly: AssemblyTitle("HCXT.Lib.BusinessService.WeixinReCallFileScaner")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("HCXT.Lib.BusinessService.WeixinReCallFileScaner")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// 将 ComVisible 设置为 false 使此程序集中的类型
// 对 COM 组件不可见。  如果需要从 COM 访问此程序集中的类型，
// 则将该类型上的 ComVisible 特性设置为 true。
[assembly: ComVisible(false)]

// 如果此项目向 COM 公开，则下列 GUID 用于类型库的 ID
[assembly: Guid("7109357e-263d-43fe-9699-420fbafa621e")]

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
[assembly: AssemblyFileVersion("1.2015.0810.1840")]

// 更新历史：
// 
// 2015-08-10
// 版 本 号：1.0.0.4
// 文件版本：1.2015.0810.1840
// 更新内容：由于支付回调在销账时经常出现超时，检查代码发现调用核心Socket时超时时长是写死的30秒(30000)。因此在配置文件增加一个[TimeOut]节点，用于配置调用电渠核心Socket超时时长（毫秒）。
// 
// 2015-05-13
// 版 本 号：1.0.0.3
// 文件版本：1.2015.0513.1550
// 更新内容：增加对回调文件的签名验证判断，以防微信支付回调记录被篡改。
//           配置文件增加API密钥[Key]节点
//           微信支付回调文件扫描服务类增加API密钥[Key]属性
// 
// 2015-05-05
// 版 本 号：1.0.0.2
// 文件版本：1.2015.0505.1846
// 更新内容：引用商城WebService。(Service_MallService)
//           配置文件增加商城接口WebService地址URL节点[Service_MallService_Url]。
//           服务类[WeixinReCallFileScaner]增加商城接口WebService地址URL属性[Service_MallService_Url]。
//           处理支付回调文件时增加判断，先判断私有域，如果私有域第一个字段为mall，则认为是商城支付的回调，需调用商城WebService处理。其他走默认，调核心处理。
// 
// 2015-03-19
// 版 本 号：1.0.0.1
// 文件版本：1.2015.0319.1152
// 更新内容：第一个版本的微信支付回调文件扫描处理服务类。