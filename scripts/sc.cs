using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Xml;
using System.Text;


namespace Net
{

    public enum lifterType
    {
        lifter0001=0, //曳引式简易升降机
        lifter0002, //强制式简易升降机
        lifter00030001, //sc双笼施工升降机01
        lifter00030002, //sc双笼施工升降机02
        lifterMax
    }
    
    public enum enumType
    {
        Type1001 = 1001, //加速度和水平度
        Type1002 = 1002, //制动距离 实时位置 运行速度
    }
  
    //服务端 发送的 电梯 数据 结构体
    public struct lifterData
    {
        public string strLifterID; //电梯ID 标识 数据是属于哪个电梯的
        public int runDir; //电梯运行方向 -1 0 1 (静止 下 上）
        public float runWz; //电梯运行的位置 cm
        public float runSpeed; //电梯运行的速度 cm/s
        public float runJsd; //加速度 g
        public float runSpdX; //x轴水平度
        public float runSpdY; //y轴水平度
        public int runKgztMax; //开关状态最大值
        public int[] runKgzt; //开关状态 -1 断线  0 
    }

    // 声明INI文件的读操作函数 GetPrivateProfileString()  

  public class ClientSocket
    {
        const int bufSize = 4096;
        private static byte[] result = new byte[bufSize];
        private static Socket clientSocket;
        private IPAddress mIp; //服务端 ip
        private IPEndPoint ip_end_point;//端口
        private string strXML = "";
        //是否已连接的标识  
        private bool IsConnected = false;
        private bool ConnectBool = true; //服务端连接
        private static Thread revThread = null;
        private static int kaiguanZtMax=26; //开关状态最大值为
        public lifterData[] runD; //保存电梯类型 
        public string[] lifterName = { "lifter0001", "lifter0002", "lifter00030001", "lifter00030002", } ;//保存电梯 对应的 物体对象 名

        //服务端ip和端口
        private string strServerIp;
        private int serverPort;

        //服务端ID
        private string strClientID;

        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, System.Text.StringBuilder retVal, int size, string filePath);

        public ClientSocket()
        {
            //模拟测试
            int lifterNum = (int)lifterType.lifterMax;
            runD = new lifterData[lifterNum];
            for (int index = 0; index < lifterNum; ++index)
            {
                runD[index].runDir = -1;
                runD[index].runSpeed = -1;
                runD[index].runKgztMax = kaiguanZtMax;
                runD[index].runKgzt = new int[kaiguanZtMax];
                for (int indexKgzt = 0; indexKgzt < kaiguanZtMax; ++indexKgzt)
                    runD[index].runKgzt[indexKgzt] = -1;
            }


            string Current;

            Current = Directory.GetCurrentDirectory();//获取当前根目录 
            string str = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            //初始化配置文件
            InitConfig(Current + "\\iniConfig\\config.ini");

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

        //连接成功与否
        public bool IsOnline()
        {
            return IsConnected;
        }

        void InitConfig(string strPath)
        {
            System.Text.StringBuilder temp = new System.Text.StringBuilder(255);
            System.Text.StringBuilder tempPort = new System.Text.StringBuilder(255);
            System.Text.StringBuilder tempID = new System.Text.StringBuilder(255);

            // section=配置节，key=键名，temp=上面，path=路径  

            GetPrivateProfileString("SERVER", "IP", "127.0.0.1", temp, 255, strPath);
            strServerIp = temp.ToString();

            
            GetPrivateProfileString("SERVER", "PORT", "22222", tempPort, 255, strPath);
            int.TryParse(tempPort.ToString(), out serverPort);

            GetPrivateProfileString("USER", "CLIENTID", "30020001", tempID, 255, strPath);
            strClientID = tempID.ToString();
        }

        /// <summary>  
        /// 连接指定IP和端口的服务器  
        /// </summary>  
        /// <param name="ip"></param>  
        /// <param name="port"></param>  
        public void ConnectServer(string ip = "127.0.0.1", int port=22222)
        {
            string curIp = ip;
            int curPort = port;
            if(string.Empty != strServerIp && serverPort != 0)
            {
                curIp = strServerIp;
                curPort = serverPort;
            }
            mIp = IPAddress.Parse(curIp);
            ip_end_point = new IPEndPoint(mIp, curPort);
            
                try
                {
                    clientSocket.Connect(ip_end_point);
                    IsConnected = true;
                    ConnectBool = false;
                    //  Debug.Log("连接服务器成功");
                   
                }
                catch (Exception ex)
                {
                    //clientSocket.enco();
                    IsConnected = false;
                    // Debug.Log("连接服务器失败");
                    //  Debug.Log(ex.ToString());
                     //5mis
                }
            if(IsConnected)
            {
                //登陆上线
                
              
               string strSend = String.Format("<?xml version='1.0' encoding ='utf8'?><body type = '4007'><clientID>{0}</clientID><lifterID>-1</lifterID><msg>1</msg></body>"
                   ,strClientID);
                strSend = string.Format("being#{0}#{1}#", "4007", strSend.Length) + strSend + "#end#";

                byte[] sendArray = System.Text.Encoding.Default.GetBytes(strSend);
                clientSocket.BeginSend(sendArray,0, sendArray.Length, SocketFlags.None
                    , new System.AsyncCallback(SendToServer), clientSocket);


                //开始异步等待接收服务端消息
                clientSocket.BeginReceive(result, 0, result.Length, SocketFlags.None
                    , new System.AsyncCallback(ReceiveFromServer), clientSocket);
            }

        
        //服务器下发数据长度  
        //int receiveLength = clientSocket.Receive(result);
      }
        //收到服务端返回消息后的回调函数
        void ReceiveFromServer(System.IAsyncResult ar)
        {
            //获取当前正在工作的Socket对象
            Socket worker = ar.AsyncState as Socket;
            int ByteRead = 0;
            try
            {
                //接收完毕消息后的字节数
                ByteRead = worker.EndReceive(ar);
            }
            catch (System.Exception ex)
            {
                
            }
            if (ByteRead > 0)
            {
 
                strXML += System.Text.Encoding.Default.GetString(result);
                if (-1 != strXML.IndexOf("#end#")) //协议收取结束
                {
                    string[] strList = strXML.Split('#');
                    foreach (string str in strList)
                    {
                        if(str.IndexOf("xml") != -1)
                            analyzeXML(str);
                    }
                        // Debug.Log(strList[3]);
                        
                    //Debug.Log(strXML);
                    strXML = "";//重置
                }
               
                //通过回调函数将消息返回给调用者
               
            }
            System.Array.Clear(result, 0, bufSize);
            //继续异步等待接受服务器的返回消息
            worker.BeginReceive(result, 0, result.Length, SocketFlags.None
                , new System.AsyncCallback(ReceiveFromServer), worker);
        }

        //发送队列
        void SendToServer(System.IAsyncResult ar)
        {
            // Debug.Log("login");
            return;
            //等3秒 发送心跳
            Thread.Sleep(3000);
            string strSend = String.Format("<?xml version='1.0' encoding ='utf8'?><body type = '4007'><clientID>{0}</clientID><lifterID>-1</lifterID><msg>2</msg></body>"
                , strClientID);

            strSend = string.Format("being#{0}#{1}#","4007", strSend.Length) + strSend + "#end#";
            byte[] sendArray = System.Text.Encoding.Default.GetBytes(strSend);
            clientSocket.BeginSend(sendArray, 0, sendArray.Length, SocketFlags.None
                , new System.AsyncCallback(SendToServer), clientSocket);
        }

        public void receiveData()
        {
           // Debug.Log("开始连接服务端");
           try
            { 
                while (ConnectBool)
                {
                    try
                    {
                        clientSocket.Connect(ip_end_point);
                        IsConnected = true;
                        ConnectBool = false;
                      //  Debug.Log("连接服务器成功");
                        break;
                    }
                    catch(Exception ex)
                    {
                        //clientSocket.enco();
                        IsConnected = false;
                       // Debug.Log("连接服务器失败");
                      //  Debug.Log(ex.ToString());
                        Thread.Sleep(5000); //5mis
                    }
                }
               // Debug.Log("获取服务端电梯数据开始");
                string strXML="";
                while (IsConnected)
                {
                    System.Array.Clear(result, 0, bufSize);
                    int receiveLength = clientSocket.Receive(result);
                    strXML += System.Text.Encoding.Default.GetString(result);
                    if(-1 != strXML.IndexOf("#end#")) //协议收取结束
                    {
                        string[] strList = strXML.Split('#');
                       // Debug.Log(strList[3]);
                        analyzeXML(strList[3]);
                    
                        strXML = "";//重置
                    }  
                }
            }catch
            {
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
          //  Debug.Log("获取服务端电梯数据结束");
        }


        public void stopThread()
        {

            if (IsConnected)
            {
                string strSend = String.Format("<?xml version='1.0' encoding ='utf8'?><body type = '4007'><clientID>{0}</clientID><lifterID>-1</lifterID><msg>0</msg></body>"
                    , strClientID);
                byte[] sendArray = System.Text.Encoding.Default.GetBytes(strSend);
                clientSocket.BeginSend(sendArray, 0, sendArray.Length, SocketFlags.None
                    , null, clientSocket);

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            ConnectBool = false;
            IsConnected = false;
        }

        ~ClientSocket()
        {
            try
            {

            }catch
            { }
        }

        public void analyzeXML(string strXML)
        {
            try
            {
                //Debug.Log(strXML);
                string strBeginBl = "belongs='";
                int beginBl = strXML.IndexOf(strBeginBl);
                int endBl = strXML.IndexOf("' type=");

                if (beginBl != -1 && endBl != -1)
                {

                    strBeginBl = strXML.Substring(beginBl + strBeginBl.Length, endBl - beginBl - strBeginBl.Length);
                }

                string lifterID = strBeginBl;

                //电梯所属
                int index = -1;
               if (lifterID.Substring(0, 4) == "0001") //曳引式简易升降机
                   index = 0;
               if (lifterID.Substring(0, 4) == "0002") //强制式简易升降机
                   index = 1;
               if (lifterID.Substring(0, 4) == "0003") //sc双笼施工升降机
               {
                   int ret = int.Parse(lifterID.Substring(lifterID.Length - 1, 1)) % 2;
                   if (ret == 0)//偶数 为第二个笼
                       index = 3;
                   else //奇数为 第一个笼
                       index = 2;
               }

                if (index > -1 && index < (int)lifterType.lifterMax)
                {
                    //记录电梯所属
                    runD[index].strLifterID = lifterID;

                    //电梯位置
                    string strBeginPos = "<data_real_pos>";
                    int beginPos = strXML.IndexOf(strBeginPos);
                    int endPos = strXML.IndexOf("</data_real_pos>");
                    if (beginPos != -1 && endPos != -1)
                    {

                        string strPos = strXML.Substring(beginPos + strBeginPos.Length, endPos - beginPos - strBeginPos.Length);

                        runD[index].runWz = float.Parse(strPos);
                       
                    }
                    //方向
                    string strBeginDir = "<data_dir>";
                    int beginDir = strXML.IndexOf(strBeginDir);
                    int endDir = strXML.IndexOf("</data_dir>");
                    if (beginDir != -1 && endDir != -1)
                    {
                         string strDir = strXML.Substring(beginDir+ strBeginDir.Length, endDir - beginDir-strBeginDir.Length);
                        
                         runD[index].runDir = int.Parse(strDir);
                    }


                    //速度
                    string strBeginSpeed = "<data_running_speed>";
                    int beginSpeed = strXML.IndexOf(strBeginSpeed);
                    int endSpeed = strXML.IndexOf("</data_running_speed>");
                    if (beginSpeed != -1 && endSpeed != -1)
                    {
                        string strSpeed = strXML.Substring(beginSpeed + strBeginSpeed.Length, endSpeed - beginSpeed - strBeginSpeed.Length);

                        runD[index].runSpeed = float.Parse(strSpeed);
                    }

                    //加速度
                    string strBeginJsd = "<data_speed>";
                    int beginJsd = strXML.IndexOf(strBeginJsd);
                    int endJsd = strXML.IndexOf("</data_speed>");
                    if (beginJsd != -1 && endJsd != -1)
                    {
                        string strJsd = strXML.Substring(beginJsd + strBeginJsd.Length, endJsd - beginJsd - strBeginJsd.Length);

                        runD[index].runJsd = float.Parse(strJsd);
                    }

                    //水平度x
                    string strBeginSpdX = "<data_levelness_x>";
                    int beginSpdX = strXML.IndexOf(strBeginSpdX);
                    int endSpdX = strXML.IndexOf("</data_levelness_x>");
                    if (beginSpdX != -1 && endSpdX != -1)
                    {
                        string strSpdX = strXML.Substring(beginSpdX + strBeginSpdX.Length, endSpdX - beginSpdX - strBeginSpdX.Length);

                        runD[index].runSpdX = float.Parse(strSpdX);
                    }

                    //水度y
                    string strBeginSpdY = "<data_levelness_y>";
                    int beginSpdY = strXML.IndexOf(strBeginSpdY);
                    int endSpdY = strXML.IndexOf("</data_levelness_y>");
                    if (beginSpdY != -1 && endSpdY != -1)
                    {
                        string strSpdY = strXML.Substring(beginSpdY + strBeginSpdY.Length, endSpdY - beginSpdY - strBeginSpdY.Length);

                        runD[index].runSpdY = float.Parse(strSpdY);
                    }

                    //获取开关状态
                    for(int indexKgzt = 0; indexKgzt < kaiguanZtMax; ++indexKgzt)
                    {
                        string strBeginKgzt = string.Format("LINK='200100{0:D2}'>",indexKgzt+1);
                        int beginKgzt = strXML.IndexOf(strBeginKgzt);
                        //int endKgzt = strXML.IndexOf("</DI>");
                        if (beginKgzt != -1 )
                        {
                            string strKgzt = strXML.Substring(beginKgzt + strBeginKgzt.Length, 1);

                            runD[index].runKgzt[indexKgzt] = int.Parse(strKgzt);
                        }
                    }
                }

            }
            catch
            {
               // Debug.Log("解析错误");
            }
        }
    }
}
public class sc : MonoBehaviour {


    enum showData
    {
        showData_title = 0, //标题显示
        showData_jiaSd, //加速度
        showData_shuiPdX,//水平度x
        showData_shuiPdY, //水平度y

        showData_lifterZt, //电梯运行状态 上 下 静止
        showData_lifterSd, //电梯运行速度 一段时间的平均速度
        showData_lifetWz, //电梯位置 -电梯位于 第几层

        showData_kaiguanSx, //开关状态 -上行
        showData_kaiguanXx, //下行
     
        showData_kaiguanQd, //电铃 启动
        showData_kaiguanJt, //急停
        showData_kaiguanXxw,//下限位
        showData_kaiguanSxw, //上限位
        showData_kaiguanJkm,//进口门
        showData_kaiguanXsq, //限速器
        showData_kaiguanCkm,//出口门
        showData_kaiguanTcm,//天窗门
        
        
        showData_max
    }
    //开关label 对应的显示 数据位置
    private char[] m_showDataKg ;
    //数据类型 关联显示控件的名字
    string[] m_showDataTitle = {"SC型升降机","加速度:", "水平度x:", "水平度y:"
            ,"电梯状态:","电梯速度:","电梯位置:","上行:","下行:","电铃/启动","急停"
            ,"下限位","上限位","进口门","限速器","出口门","天窗门"};

    //对应 enum lifterType 电梯类型
    string[] m_showDataWnd = { "","","wnd01/grid01",""};


    //     public GameObject lifter_two;
    //     public GameObject lifter_three01;
    //     public GameObject lifter_three02;
    public float m_maxUp = 18; //向上 最大位置
    public float m_minDown = 0; //向下 最小位置
    //参考对象 大楼
    public Transform m_targetLou01;
    public Transform m_targetLou02;
    public Transform m_targetLou00; //地面
    private Net.ClientSocket client;
    // Use this for initialization

    private float[] m_lifetSpeed;

    //电梯起始位置
    private float m_lifterBeginWz = 0.01963577f;

    //控件字体设置
    private Font showDataFont;
    private int showDataSize;
    private Camera uiCamera;

    struct UIShowData
    {
      public  UITexture m_textureShow;
      public  UILabel m_labelShow;
    }

    //labels map
    private UIShowData[][]  m_showLabelsAry;// = new showLabels[(int)showData.showData_max];
                                        //texture map

    private int m_aryOneSize; //第一维大小
    private int m_aryTwoSize; //第二维大小

    //开关两个状态的开关图片
    Texture m_textureGray;
    Texture m_textureGreen;
    Texture m_textureRed;


//动态创建 控件
void InitMyWnd()
    {
        //暂时不用,有些问题 还没研究清楚
        return;
        var titleLabel = GameObject.Find("wnd01");
        Color labelColor = Color.black;
        int labelDepth = 5; //深度
        int labelH = 70; //高 像素
        int labelW = 320; //宽

        //UIFont showDataFont = Resources.Load("font/arial", typeof(UIFont)) as UIFont;

        UILabel label = NGUITools.AddChild<UILabel>(titleLabel);

        label.text = "我曹";
        label.fontSize = showDataSize;
        label.trueTypeFont = showDataFont;
        label.color = labelColor;
        label.depth = labelDepth;
        label.height = labelH;
        label.width = labelW;

        if (uiCamera != null)
            label.transform.position = uiCamera.ScreenToWorldPoint(new Vector3(-160, 470, 0));
    }

    //
    void InitLabelMaps()
    {
        
        
        m_aryOneSize = m_showDataWnd.Length;
        m_aryTwoSize = (int)showData.showData_max;

        m_showDataKg = new char[m_aryTwoSize];
        m_showDataKg[(int)showData.showData_kaiguanSx] = (char)0;
        m_showDataKg[(int)showData.showData_kaiguanXx] = (char)1;
        m_showDataKg[(int)showData.showData_kaiguanJt] = (char)5;
        m_showDataKg[(int)showData.showData_kaiguanQd] = (char)6;
        m_showDataKg[(int)showData.showData_kaiguanSxw] = (char)7;
        m_showDataKg[(int)showData.showData_kaiguanXxw] = (char)8;
        m_showDataKg[(int)showData.showData_kaiguanXsq] = (char)9;
        m_showDataKg[(int)showData.showData_kaiguanJkm] = (char)10;
        m_showDataKg[(int)showData.showData_kaiguanTcm] = (char)11;
        m_showDataKg[(int)showData.showData_kaiguanCkm] = (char)12;
        if (m_aryOneSize > 0)
        {
            m_showLabelsAry = new UIShowData[m_aryOneSize][];
            
            string strLabelName, strWndName;
            for (int index = 0; index < m_aryOneSize; ++index)
            {
                strWndName = m_showDataWnd[index];
                if (strWndName == "")
                {
                    m_showLabelsAry[index] = null;
                    
                    continue;
                }
                   
                m_showLabelsAry[index] = new UIShowData[m_aryTwoSize];
               
                for (int indexTwo = 0; indexTwo < m_aryTwoSize
                    ; ++indexTwo)
                {
                    //默认初始化
                    m_showLabelsAry[index][indexTwo] = new UIShowData();
                    m_showLabelsAry[index][indexTwo].m_labelShow = null;
                    m_showLabelsAry[index][indexTwo].m_textureShow = null;

                    //显示数据的label
                    strLabelName = string.Format("{0}/title_{1}_1", strWndName, indexTwo);

           
                    var gameObj = GameObject.Find(strLabelName);
                    if (gameObj != null)
                    {
                        if(indexTwo<(int)showData.showData_kaiguanSx)
                        {
                            UILabel titleLabel = gameObj.GetComponent<UILabel>();
                            if (titleLabel != null)
                            {
                                m_showLabelsAry[index][indexTwo].m_labelShow = titleLabel;
                            }
                        }
                        else if(indexTwo >= (int)showData.showData_kaiguanSx 
                            && indexTwo < (int)showData.showData_max)
                        {
                            UITexture titleLabel = gameObj.GetComponent<UITexture>();
                            if (titleLabel != null)
                            {
                                m_showLabelsAry[index][indexTwo].m_textureShow = titleLabel;
                            }
                        }
                       
                    }
                    
                    //设置显示标题
                    strLabelName = string.Format("{0}/title_{1}_0", strWndName, indexTwo);
                    var gameTitile = GameObject.Find(strLabelName);
                    if (gameTitile != null)
                    {
                        UILabel titleLabel = gameTitile.GetComponent<UILabel>();
                        if (titleLabel != null)
                        {
                            titleLabel.text = m_showDataTitle[indexTwo];
                        }
                    }
                }

            }
        }
        m_textureGray = Resources.Load("kaiguan/gray", typeof(Texture)) as Texture;
        m_textureGreen = Resources.Load("kaiguan/green", typeof(Texture)) as Texture;
        m_textureRed = Resources.Load("kaiguan/red", typeof(Texture)) as Texture;

         for (int index = 0; index < m_aryOneSize; ++index)
         {
             if (m_showLabelsAry[index] == null)
                 continue;
             for (int indexTwo = 0; indexTwo < m_aryTwoSize; ++indexTwo)
             {
                 
                 UITexture label = m_showLabelsAry[index][indexTwo].m_textureShow;
                 if (label != null)
                     label.mainTexture = m_textureGray;
                 
             }
         }


    }
    void Start()
    {

        //初始化 显示控件
        InitLabelMaps();


        //初始化电梯运动 参考数据
        m_lifetSpeed = new float[(int)Net.lifterType.lifterMax];




        //电梯控制
        client = new Net.ClientSocket();
        client.ConnectServer();
    }
        
    //展示数据,电梯运动
    void showDataFun()
    {

        for (int indexWnd = 0; indexWnd < (int)Net.lifterType.lifterMax; ++indexWnd)
        {
            if (m_showLabelsAry[indexWnd] == null)
                continue;
            int runDir = client.runD[indexWnd].runDir; //电梯运行方向 -1 0 1 (静止 下 上）
            float runWz = (float)client.runD[indexWnd].runWz; ////电梯运行的位置 cm
            float runSpeed = (float)client.runD[indexWnd].runSpeed; ////电梯运行 速度 cm/s
            float speedTemp = m_lifetSpeed[indexWnd]; //参看数据//数据有效 电梯运动
            
            //非静止状态
            if (speedTemp != runWz)
            {
                string strObjectName = client.lifterName[indexWnd];

                GameObject activeLifter = GameObject.FindGameObjectWithTag(strObjectName);  //选择当前活动的电梯

                if (activeLifter == null)
                    return;
                m_lifetSpeed[indexWnd] = runWz;
                //传递过来的数据单位为cm,模型默认单位是m 
                runWz = runWz / 100.0f;
                runSpeed = runSpeed / 100.0f;

                //0.软件刚启动,定位电梯位置
                if (speedTemp == 0f)
                {
                    //1.通过位置平移实现
                    activeLifter.transform.Translate(Vector3.up * runWz, Space.World);
                }
 

                //Debug.Log(runSpeed);

                //1.通过位置平移实现
                // activeLifter.transform.Translate(Vector3.up * (translation - activeLifter.transform.position.y + 1.9f), Space.World);
                //Debug.Log(runWz - activeLifter.transform.position.y + 1.9f);
                //2.通过速度 实现电梯移动的效果
                if (runDir == 1) //上
                {
                    activeLifter.transform.Translate(Vector3.up * (runSpeed * Time.deltaTime + m_lifterBeginWz), Space.World);
                    Debug.Log(Time.deltaTime);
                }
                else if (runDir == 0) //下
                {
                    activeLifter.transform.Translate(Vector3.down * (runSpeed * Time.deltaTime + m_lifterBeginWz), Space.World);
                    Debug.Log(Time.deltaTime);
                }
            }
            else
            {
                //静止时
                string strObjectName = client.lifterName[indexWnd];

                GameObject activeLifter = GameObject.FindGameObjectWithTag(strObjectName);  //选择当前活动的电梯

                if (activeLifter == null)
                    return;
                runWz = runWz / 100.0f;
                float moveJl = (runWz - activeLifter.transform.position.y + 1.963577f);
                activeLifter.transform.Translate(Vector3.up * moveJl, Space.World);
                    continue;
               
            }
        }

        if (Time.frameCount % 50 == 0)
        {
            for (int indexWnd = 0; indexWnd < (int)Net.lifterType.lifterMax; ++indexWnd)
            {
                if (m_showLabelsAry[indexWnd] == null)
                    continue;
                /*
                int runDir = client.runD[indexWnd].runDir; //电梯运行方向 -1 0 1 (静止 下 上）
                float runWz = (float)client.runD[indexWnd].runWz; ////电梯运行的位置 cm
                float runSpeed = (float)client.runD[indexWnd].runSpeed; ////电梯运行 速度 cm/s
                float speedTemp = m_lifetSpeed[indexWnd]; //参看数据
                //数据有效 电梯运动
                if (runWz != -1 && speedTemp != runWz)
                {
                    m_lifetSpeed[indexWnd] = runWz;
                    //传递过来的数据单位为cm,模型默认单位是m 
                    runWz = runWz / 100.0f;
                    runSpeed = runSpeed / 100.0f;
                    //Debug.Log(runSpeed);
                    string strObjectName = client.lifterName[indexWnd];

                    GameObject activeLifter = GameObject.FindGameObjectWithTag(strObjectName);  //选择当前活动的电梯

                    if (activeLifter == null)
                        return;
                    //1.通过位置平移实现
                    // activeLifter.transform.Translate(Vector3.up * (translation - activeLifter.transform.position.y + 1.9f), Space.World);

                    //2.通过速度 实现电梯移动的效果
                    if (runDir == 1) //上
                    {
                        activeLifter.transform.Translate(Vector3.up * runSpeed *Time.deltaTime, Space.World);
                    }
                    else if (runDir == 0) //下
                    {
                        activeLifter.transform.Translate(Vector3.down * runSpeed * Time.deltaTime, Space.World);
                    }
                    else
                    {
                        //activeLifter.transform.Translate(0, Space.World);
                    }
                }
                */


                //展示状态数据
                int tempWz;

                {
                    //开关显示
                    for(int indexLabel = (int)showData.showData_kaiguanSx
                        ; indexLabel < (int)showData.showData_max;++indexLabel)
                    {
                        UITexture dataLabel = m_showLabelsAry[indexWnd][indexLabel].m_textureShow;
                        tempWz = m_showDataKg[indexLabel];

                        if (tempWz >= 0 && tempWz < (int)showData.showData_max
                            && dataLabel != null) //有效
                        {
                            Texture strText;
                            int kaiguanData; //0 打开  1闭合
                            kaiguanData = client.runD[indexWnd].runKgzt[tempWz];

                            //上、下 限位  、限速开关 是相反逻辑
                            if (indexLabel == (int)showData.showData_kaiguanXxw
                                || indexLabel == (int)showData.showData_kaiguanSxw
                                || indexLabel == (int)showData.showData_kaiguanXsq)
                            {
                                kaiguanData = 1 - kaiguanData;
                            }

                            if (kaiguanData == 0)
                                strText = m_textureRed;
                            else if (kaiguanData == 1)
                                strText = m_textureGreen;
                            else
                                strText = m_textureGray;

                            dataLabel.mainTexture = strText;
                        }
                    }
                    //加速度
                    UILabel dataLabelTemp = m_showLabelsAry[indexWnd][(int)showData.showData_jiaSd].m_labelShow;
                    if(dataLabelTemp != null)
                    {
                        float jiaSd = client.runD[indexWnd].runJsd;
                        dataLabelTemp.text = string.Format("{0:N}g",jiaSd<0?0:jiaSd);
                    }
                    //水平度 x
                     dataLabelTemp = m_showLabelsAry[indexWnd][(int)showData.showData_shuiPdX].m_labelShow;
                    if (dataLabelTemp != null)
                    {
                        dataLabelTemp.text = string.Format("{0:N}°", client.runD[indexWnd].runSpdX);
                    }
                    //y
                     dataLabelTemp = m_showLabelsAry[indexWnd][(int)showData.showData_shuiPdY].m_labelShow;
                    if (dataLabelTemp != null)
                    {
                        dataLabelTemp.text = string.Format("{0:N}°", client.runD[indexWnd].runSpdY);
                    }

                    //运行状态
                     dataLabelTemp = m_showLabelsAry[indexWnd][(int)showData.showData_lifterZt].m_labelShow;
                    if (dataLabelTemp != null)
                    {
                        int dir = client.runD[indexWnd].runDir;
                        string strDir;
                        if (dir == 1) //上
                            strDir = "上升";
                        else if (dir == 0)
                            strDir = "下降";
                        else
                            strDir = "静止";
                        dataLabelTemp.text = strDir;
                    }
                    //速度
                    dataLabelTemp = m_showLabelsAry[indexWnd][(int)showData.showData_lifterSd].m_labelShow;
                    if (dataLabelTemp != null)
                    {
                        float sdTemp = client.runD[indexWnd].runSpeed;
                        dataLabelTemp.text = string.Format("{0:N}cm/s", sdTemp<0?0:sdTemp);
                    }
                    //位置
                    dataLabelTemp = m_showLabelsAry[indexWnd][(int)showData.showData_lifetWz].m_labelShow;
                    if (dataLabelTemp != null)
                    {
                        dataLabelTemp.text = string.Format("{0:N}cm", client.runD[indexWnd].runWz);
                    }
                }
            }
        }

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //控制移动  
        //设置移动范围
        try
        {

//             string strObjectName = client.lifterName[2];
// 
//             GameObject activeLifter = GameObject.FindGameObjectWithTag(strObjectName);  //选择当前活动的电梯
// 
//             if (activeLifter == null)
//                 return;
//             //1.通过位置平移实现
//             // activeLifter.transform.Translate(Vector3.up * (translation - activeLifter.transform.position.y + 1.9f), Space.World);
// 
//             //2.通过速度 实现电梯移动的效果
//            // if (runDir == 1) //上
//             {
//                 
//                 activeLifter.transform.Translate(Vector3.up *( 0.25f * Time.deltaTime+m_lifterBeginWz), Space.World);
//             }

            showDataFun();
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    void OnDisable()
    {
        if(client != null)
        client.stopThread();
        //Debug.Log("disable");
    }
    void OnDestroy()
    {
        //Debug.Log("destroy");
    }
}
