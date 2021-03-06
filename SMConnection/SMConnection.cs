﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections;
using System.Threading;

namespace StateManager
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class SMConnection : IState //各种连接的抽象基类MarshalByRefObject
    {
        public SObject so;
        public string ConnectionString="";
        public string ConnectionStringSimu="";
        public bool ConnectionStringUsedEncrypt=false;
        public bool Simulate=false;
        public int ReConnectDelay = 10000;
        public int ConnectTimeOut = 3000;
        public int ReadTimeOut = 10000;
        public SMConnection()
        {
        }

        public string UsedConnectionStr
        {
            get
            {
                string ConnectionStr = Simulate ? ConnectionStringSimu : ConnectionString;
                try
                {
                    ConnectionStr = ConnectionStringUsedEncrypt ? EnDeCrypt.Decrypt(ConnectionStr) : ConnectionStr;
                }
                catch (Exception E)
                {
                    throw new Exception(string.Format("{0}\n\n{1}加密结果为：\n{2}",E.Message ,ConnectionStr ,EnDeCrypt.Encrypt(ConnectionStr)));
                }
                ConnectionStr = ConnectionStr.Replace("\\\\", "\\");
                return ConnectionStr;
            }
        }

        private bool connected;
        public virtual bool Connected
        {
            get { return connected; }
            set { connected = value; }
        }
        public void Connect()  //事务流用void抛异常更方便，可同时返回E.message、是否成功两个结果，同时可以向外部暴出系统异常。
        {
            Connected = false;
            so.Status = "开始连接..";
            so.Update();

            try
            {
                DoConnect(UsedConnectionStr);
                Connected = true;
                so.Status = "已连接";
                so.Update();
            }
            catch(Exception E)
            {
                so.Status = E.Message;
                so.Update();
                throw;
            }
        }

        public void DisConnect()
        {
            so.Status = "开始断开..";
            so.Update();
            try
            {
                DoDisConnect();
                Connected = false;
                so.Status = "未连接";
                so.Update();
            }
            catch (Exception E)
            {
                so.Status = E.Message;
                so.Update();
                throw;
            }
        }

        public abstract void DoCreateConObj(string UsedConnectionStr);

        public abstract void DoConnect(string UsedConnectionStr);

        public abstract void DoDisConnect();

        public void StateInit(SObject so)
        {
            this.so = so;
            if (so.JObject.ContainsKey("ConnectionString"))
                ConnectionString = so.JObject["ConnectionString"].ToString();
            if (so.JObject.ContainsKey("ConnectionStringSimu"))
                ConnectionStringSimu = so.JObject["ConnectionStringSimu"].ToString();
            if (so.JObject.ContainsKey("ConnectionStringUsedEncrypt"))
                ConnectionStringUsedEncrypt = (bool)so.JObject["ConnectionStringUsedEncrypt"];
            if (so.JObject.ContainsKey("Simulate"))
                Simulate = (bool)so.JObject["Simulate"];
            if (so.JObject.ContainsKey("Simulate"))
                ReConnectDelay = (int)so.JObject["ReConnectDelay"];
            if (so.JObject.ContainsKey("ConnectTimeOut"))
                ConnectTimeOut = (int)so.JObject["ConnectTimeOut"];
            if (so.JObject.ContainsKey("ReadTimeOut"))
                ReadTimeOut = (int)so.JObject["ReadTimeOut"];
            so.Status = "未连接";
            so.Update();
            Connected = false;

            DoCreateConObj(UsedConnectionStr);
        }

        public void StateFInit(SObject so)
        {
        }

        public abstract object Form
        {
            get;
        }

        public void StateHandle(ref SObject so)
        {
            switch (so.State)
            {
                case "":
                    so.SetNextState("网络检测", 0);
                    break;
                case "网络检测":
                    if (!Connected)
                    {
                        try
                        {
                            Connect();
                        }
                        catch
                        {
                        }
                    }
                    so.RepeatState(ReConnectDelay);
                    break;
            }
        }

    }

}
