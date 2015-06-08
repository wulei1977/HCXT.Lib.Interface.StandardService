using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HCXT.Lib.WeiXinPay.Model;

namespace HCXT.Lib.BusinessService.WeixinMsgFileScaner
{
    /// <summary>
    /// 发送微信下行消息类
    /// </summary>
    public class SendMessage
    {
        /// <summary>发送客服下行消息的URL</summary>
        public const string SendUrl = "https://api.weixin.qq.com/cgi-bin/message/custom/send?access_token=";
        public static void SendText(string msg, string accessToken, string openId)
        {
            RequestKfMsg_text mod = new RequestKfMsg_text();
            mod.msgtype = "text";
            mod.text = new RequestKfMsg_text.TextMessage();
            mod.text.content = msg;
            mod.touser = openId;

        }
    }
}
