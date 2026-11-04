using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Resources;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Configuration;



using Kuaff.CardResouces;
using Kuaff.ModelResources;
using Kuaff.OperaResources;

using Kuaff.TractorFere;


namespace Kuaff.Tractor
{
    internal partial class MainForm : Form
    {
        #region 变量声明
        //缓冲区图像
        internal Bitmap bmp = null;
        //原始背景图片
        internal Bitmap image = null;
        //启动菜单
        private bool showingStartMenu = true;
        private string startMenuMessage = "";
        private readonly Rectangle startMenuPanelRect = new Rectangle(164, 58, 301, 354);
        private readonly Rectangle startMenuStartRect = new Rectangle(202, 185, 225, 46);
        private readonly Rectangle startMenuLoadRect = new Rectangle(202, 243, 225, 46);
        private readonly Rectangle startMenuExitRect = new Rectangle(202, 301, 225, 46);
      

        //*状态
        //当前的状态
        internal CurrentState currentState ;
        //当前的Rank,代表当前牌局的Rank,0代表实际的牌局2.....11代表K,12代表A,53代表打王
        internal int currentRank = 0;
        //是否是新开始的游戏
        internal bool isNew = true;

        //亮牌的次数
        internal int showSuits = 0;
        //谁亮的牌
        internal int whoShowRank = 0;


        //*发牌序列
        //得到一次发牌的序列,dpoker时发牌的帮助类，pokerList是每个人手中的牌的列表
        internal DistributePokerHelper dpoker = null;
        internal ArrayList[] pokerList = null;

        //每个人手中解析好的牌
        internal CurrentPoker[] currentPokers = { new CurrentPoker(), new CurrentPoker(), new CurrentPoker(), new CurrentPoker() };
        //画图的次数（仅在发牌时使用）
        internal int currentCount = 0;
        //当前一轮各家的出牌情况
        internal ArrayList[] currentSendCards = new ArrayList[4];
        //应该谁出牌
        internal int whoseOrder = 0;//0未定,1我，2对家，3西家,4东家
        //一次出来中谁最先开始出的牌
        internal int firstSend = 0;

        //*辅助变量
        //当前手中牌的坐标
        internal ArrayList myCardsLocation = new ArrayList();
        //当前手中牌的数值
        internal ArrayList myCardsNumber = new ArrayList();
        //当前手中牌的是否被点出
        internal ArrayList myCardIsReady = new ArrayList();
        // Hint button state
        internal bool hintActive = false;
        private ArrayList hintPreviousReady = new ArrayList();
        //当前扣底的牌
        internal ArrayList send8Cards = new ArrayList();

        //*画我的牌的辅助变量
        //画牌顺序
        internal int cardsOrderNumber = 0;

        //确定程序休眠的最长时间
        internal long sleepTime;
        internal long sleepMaxTime = 2000;
        internal CardCommands wakeupCardCommands;

        //*绘画辅助类
        //DrawingForm变量
        internal DrawingFormHelper drawingFormHelper = null;
        internal　CalculateRegionHelper calculateRegionHelper = null;

        //记录本次得分
        internal int Scores = 0;

       
        //游戏设置
        internal GameConfig gameConfig = new GameConfig();

        //出牌时目前牌最大的那一家
        internal int whoIsBigger = 0;


        //音乐文件
        private string musicFile = "";
        //牌面图案
        internal Bitmap[] cardsImages = new Bitmap[54];

        //出牌算法
        internal object[] UserAlgorithms = { null, null, null, null };

        //当前一局已经出的牌
        internal CurrentPoker[] currentAllSendPokers = { new CurrentPoker(), new CurrentPoker(), new CurrentPoker(), new CurrentPoker() };

        #endregion // 变量声明

    
        internal MainForm()
        {
            InitializeComponent();
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.StandardDoubleClick, true);

           
            //读取程序配置
            InitAppSetting();
            
            notifyIcon.Text = Text;
            //背景统一由缓冲区绘制，避免全屏时窗口自行平铺造成重影
            BackColor = Color.White;
        
            //变量初始化
            bmp = new Bitmap(LogicalWidth * RenderScale, LogicalHeight * RenderScale);
            
            
            drawingFormHelper = new DrawingFormHelper(this);
            calculateRegionHelper = new CalculateRegionHelper(this);


            for (int i = 0; i < 54; i++)
            {
                cardsImages[i] = null; //初始化
            }

            ShowStartMenu();
            WindowState = FormWindowState.Maximized;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            WindowState = FormWindowState.Maximized;

            if (IsHandleCreated)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    if (showingStartMenu)
                    {
                        ShowStartMenu();
                    }
                    else
                    {
                        ApplyDisplayMode();
                    }
                });
            }
        }

        private void ShowStartMenu()
        {
            if (bmp == null || drawingFormHelper == null)
            {
                return;
            }

            PrepareRenderBuffer();
            using (Graphics g = Graphics.FromImage(bmp))
            {
                drawingFormHelper.DrawBackground(g);
                MarkRenderScaled(g);
                DrawStartMenu(g);
            }

            Invalidate();
        }

        private void DrawStartMenu(Graphics g)
        {
            Rectangle panel = startMenuPanelRect;

            using (GraphicsPath shadowPath = CreateRoundedRectangle(new Rectangle(panel.X + 6, panel.Y + 7, panel.Width, panel.Height), 16))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            using (GraphicsPath panelPath = CreateRoundedRectangle(panel, 16))
            using (LinearGradientBrush panelBrush = new LinearGradientBrush(panel,
                Color.FromArgb(242, 24, 91, 50),
                Color.FromArgb(248, 8, 54, 28), 90f))
            using (Pen outerPen = new Pen(Color.FromArgb(240, 218, 185, 108), 2f))
            {
                g.FillPath(panelBrush, panelPath);
                g.DrawPath(outerPen, panelPath);
            }

            Rectangle innerRect = Rectangle.Inflate(panel, -7, -7);
            using (GraphicsPath innerPath = CreateRoundedRectangle(innerRect, 11))
            using (Pen innerPen = new Pen(Color.FromArgb(125, 255, 238, 188), 1f))
            {
                g.DrawPath(innerPen, innerPath);
            }

            using (Font titleFont = new Font("Microsoft YaHei", 36f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (Font subtitleFont = new Font("Microsoft YaHei", 14f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(255, 249, 226, 166)))
            using (SolidBrush subtitleBrush = new SolidBrush(Color.FromArgb(225, 226, 245, 221)))
            using (StringFormat centered = new StringFormat())
            {
                centered.Alignment = StringAlignment.Center;
                centered.LineAlignment = StringAlignment.Center;
                g.DrawString("双 扣", titleFont, titleBrush, new RectangleF(panel.X, panel.Y + 17, panel.Width, 56), centered);
                g.DrawString("四人双副牌拖拉机", subtitleFont, subtitleBrush, new RectangleF(panel.X, panel.Y + 77, panel.Width, 30), centered);
            }

            using (Pen linePen = new Pen(Color.FromArgb(120, 236, 207, 129), 1f))
            {
                g.DrawLine(linePen, panel.X + 46, panel.Y + 120, panel.Right - 46, panel.Y + 120);
            }

            DrawStartMenuButton(g, startMenuStartRect, "开始新游戏", true);
            DrawStartMenuButton(g, startMenuLoadRect, "读取牌局", false);
            DrawStartMenuButton(g, startMenuExitRect, "退出游戏", false);

            string status = string.IsNullOrEmpty(startMenuMessage) ? "选择一项开始" : startMenuMessage;
            Color statusColor = string.IsNullOrEmpty(startMenuMessage)
                ? Color.FromArgb(180, 219, 237, 217)
                : Color.FromArgb(255, 255, 218, 128);
            using (Font statusFont = new Font("Microsoft YaHei", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush statusBrush = new SolidBrush(statusColor))
            using (StringFormat statusFormat = new StringFormat())
            {
                statusFormat.Alignment = StringAlignment.Center;
                statusFormat.LineAlignment = StringAlignment.Center;
                g.DrawString(status, statusFont, statusBrush, new RectangleF(panel.X + 20, panel.Y + 296, panel.Width - 40, 30), statusFormat);
            }
        }

        private void DrawStartMenuButton(Graphics g, Rectangle rect, string text, bool primary)
        {
            Rectangle shadowRect = new Rectangle(rect.X, rect.Y + 3, rect.Width, rect.Height);
            Color topColor;
            Color bottomColor;
            Color borderColor;
            Color textColor;

            if (primary)
            {
                topColor = Color.FromArgb(255, 247, 224, 161);
                bottomColor = Color.FromArgb(255, 213, 175, 84);
                borderColor = Color.FromArgb(255, 255, 244, 201);
                textColor = Color.FromArgb(255, 41, 65, 35);
            }
            else
            {
                topColor = Color.FromArgb(255, 47, 124, 72);
                bottomColor = Color.FromArgb(255, 27, 86, 48);
                borderColor = Color.FromArgb(235, 216, 184, 106);
                textColor = Color.FromArgb(255, 255, 245, 213);
            }

            using (GraphicsPath shadowPath = CreateRoundedRectangle(shadowRect, 9))
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                g.FillPath(shadowBrush, shadowPath);
            }

            using (GraphicsPath buttonPath = CreateRoundedRectangle(rect, 9))
            using (LinearGradientBrush buttonBrush = new LinearGradientBrush(rect, topColor, bottomColor, 90f))
            using (Pen borderPen = new Pen(borderColor, 1.5f))
            {
                g.FillPath(buttonBrush, buttonPath);
                g.DrawPath(borderPen, buttonPath);
            }

            using (Font font = new Font("Microsoft YaHei", 19f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush textBrush = new SolidBrush(textColor))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString(text, font, textBrush, rect, format);
            }
        }

        private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void InitAppSetting()
        {
            //没有配置文件，则从config文件中读取
            if (!File.Exists("gameConfig"))
            {
                AppSettingsReader reader = new AppSettingsReader();
                try
                {
                    Text = (String)reader.GetValue("title", typeof(String));
                }
                catch (Exception ex)
                {
                    Text = "拖拉机大战";
                }

                try
                {
                    gameConfig.MustRank = (String)reader.GetValue("mustRank", typeof(String));
                }
                catch (Exception ex)
                {
                    gameConfig.MustRank = ",3,8,11,12,13,";
                }

                try
                {
                    gameConfig.IsDebug = (bool)reader.GetValue("debug", typeof(bool));
                }
                catch (Exception ex)
                {
                    gameConfig.IsDebug = false;
                }

                try
                {
                    gameConfig.BottomAlgorithm = (int)reader.GetValue("bottomAlgorithm", typeof(int));
                }
                catch (Exception ex)
                {
                    gameConfig.BottomAlgorithm = 1;
                }
            }
            else
            {
                //实际从gameConfig文件中读取
                Stream stream = null;
                try
                {
                    IFormatter formatter = new BinaryFormatter();
                    stream = new FileStream("gameConfig", FileMode.Open, FileAccess.Read, FileShare.Read);
                    gameConfig = (GameConfig)formatter.Deserialize(stream);
                    
                }
                catch (Exception ex)
                {
                    
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }

            //未序列化的值
            AppSettingsReader myreader = new AppSettingsReader();
            gameConfig.CardsResourceManager = Kuaff_Cards.ResourceManager;
            try
            {
                String bkImage = (String)myreader.GetValue("backImage", typeof(String));
                image = new Bitmap(bkImage);
                KuaffToolStripMenuItem.CheckState = CheckState.Unchecked;

            }
            catch (Exception ex)
            {
                image = global::Kuaff.Tractor.Properties.Resources.Backgroud;
            }

            try
            {
                Text = (String)myreader.GetValue("title", typeof(String));
            }
            catch (Exception ex)
            {

            }

            gameConfig.CardImageName = "";

            if (gameConfig.IsDebug)
            {
                RobotToolStripMenuItem.CheckState = CheckState.Checked;
            }

        }



        #region 窗口事件处理程序

        internal void MenuItem_Click(object sender, EventArgs e)
        {

            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("退出"))
            {
                this.Close();
            }

            if (menuItem.Text.Equals("开始新游戏"))
            {
                showingStartMenu = false;
                PauseGametoolStripMenuItem.Text = "暂停游戏";


                //新游戏初始状态，我家和敌方都从2开始，令牌为开始发牌
                currentState = new CurrentState(0, 0, 0, 0,0,0,CardCommands.ReadyCards);
                currentRank = 0;

                isNew = true;
                whoIsBigger = 0;

                //初始化
                init();

                //开始定时器，进行发牌
                timer.Start();
            }

        }


        //初始化
        internal void init()
        {
            //按当前窗口比例重建缓冲区，保证全屏时铺满且不裁切
            PrepareRenderBuffer();
            //每次初始化都重绘背景
            Graphics g = Graphics.FromImage(bmp);
            MarkRenderScaled(g);
            drawingFormHelper.DrawBackground(g);


            //发一次牌
            dpoker = new DistributePokerHelper();
            pokerList = dpoker.Distribute();

            //每个人手中的牌清空,准备摸牌
            currentPokers[0].Clear();
            currentPokers[1].Clear(); 
            currentPokers[2].Clear();
            currentPokers[3].Clear();

            //清空已发送的牌
            currentAllSendPokers[0].Clear();
            currentAllSendPokers[1].Clear();
            currentAllSendPokers[2].Clear();
            currentAllSendPokers[3].Clear();


            //为每个人的currentPokers设置Rank
            currentPokers[0].Rank = currentRank;
            currentPokers[1].Rank = currentRank;
            currentPokers[2].Rank = currentRank;
            currentPokers[3].Rank = currentRank;
            currentPokers[0].Suit = 0;
            currentPokers[1].Suit = 0;
            currentPokers[2].Suit = 0;
            currentPokers[3].Suit = 0;


            currentSendCards[0] = new ArrayList();
            currentSendCards[1] = new ArrayList();
            currentSendCards[2] = new ArrayList();
            currentSendCards[3] = new ArrayList();

            //
            myCardsLocation= new ArrayList();
            myCardsNumber= new ArrayList();
            myCardIsReady= new ArrayList();
            hintActive = false;
            hintPreviousReady = new ArrayList();
            send8Cards= new ArrayList();


            //设置命令
            currentState.CurrentCardCommands = CardCommands.ReadyCards;
            currentState.Suit = 0;
        

            //设置还未发牌,循环25次将牌发完
            currentCount = 0;

            //目前不可以反牌
            showSuits = 0;
            whoShowRank = 0;

            //得分清零
            Scores = 0;
            

            //绘制Sidebar
            drawingFormHelper.DrawSidebar(g);
            //绘制东南西北
            drawingFormHelper.DrawOtherMaster(g, 0, 0);
            
            if (currentState.Master != 0)
            {
                drawingFormHelper.DrawMaster(g, currentState.Master, 1);
                drawingFormHelper.DrawOtherMaster(g, currentState.Master, 1);
            }

            //绘制Rank
            drawingFormHelper.DrawRank(g,currentState.OurCurrentRank,true,false);
            drawingFormHelper.DrawRank(g, currentState.OpposedCurrentRank, false, false);

            //绘制花色
            drawingFormHelper.DrawSuit(g, 0, true, false);
            drawingFormHelper.DrawSuit(g, 0, false, false);

            send8Cards = new ArrayList();
            //调整花色
            if (currentRank == 53)
            {
                currentState.Suit = 5;
            }

            whoIsBigger = 0;

            //如果设置了游戏截止，则停止游戏
            if (gameConfig.WhenFinished > 0)
            {
                
                bool b = false;

                if ((currentState.OurTotalRound + 1) > gameConfig.WhenFinished) 
                {
                    b = true;
                }
                if ((currentState.OpposedTotalRound + 1) > gameConfig.WhenFinished)
                {
                    b = true;
                }
                if (b)
                {
                    timer.Stop();
                    PauseGametoolStripMenuItem.Text = "继续游戏";
                    PauseGametoolStripMenuItem.Image = Properties.Resources.MenuResume;
                }
            }
        }

       

        //逻辑分辨率（所有绘制坐标以此为准）
        internal const int LogicalWidth = 629;
        internal const int LogicalHeight = 470;

        //超采样倍数：缓冲区按逻辑尺寸的 N 倍绘制，缩放时更清晰
        internal const int RenderScale = 4;

        //当前画布尺寸：比例与窗口一致，保证铺满且不留黑边
        internal int CanvasWidth = LogicalWidth;
        internal int CanvasHeight = LogicalHeight;

        //牌桌内容在画布中的居中偏移
        internal int ContentOffsetX = 0;
        internal int ContentOffsetY = 0;

        //按窗口比例准备缓冲区
        internal void PrepareRenderBuffer()
        {
            int cw = ClientRectangle.Width;
            int ch = ClientRectangle.Height;
            int top = (menuStrip != null && menuStrip.Visible) ? menuStrip.Height : 0;
            ch -= top;

            int w = LogicalWidth;
            int h = LogicalHeight;
            if (cw > 0 && ch > 0)
            {
                double a = (double)cw / ch;
                if (a >= (double)LogicalWidth / LogicalHeight)
                {
                    h = LogicalHeight;
                    w = (int)Math.Round(LogicalHeight * a);
                }
                else
                {
                    w = LogicalWidth;
                    h = (int)Math.Round(LogicalWidth / a);
                }
            }

            CanvasWidth = Math.Max(LogicalWidth, w);
            CanvasHeight = Math.Max(LogicalHeight, h);
            ContentOffsetX = (CanvasWidth - LogicalWidth) / 2;
            ContentOffsetY = (CanvasHeight - LogicalHeight) / 2;

            int needW = CanvasWidth * RenderScale;
            int needH = CanvasHeight * RenderScale;
            if (bmp == null || bmp.Width != needW || bmp.Height != needH)
            {
                if (bmp != null)
                {
                    bmp.Dispose();
                }
                bmp = new Bitmap(needW, needH);
            }
        }

        //逻辑坐标转缓冲区像素坐标（用于 Clone 等不受变换影响的操作）
        internal Rectangle LogicalToDevice(Rectangle r)
        {
            return new Rectangle((r.X + ContentOffsetX) * RenderScale, (r.Y + ContentOffsetY) * RenderScale, r.Width * RenderScale, r.Height * RenderScale);
        }

        private void MainForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle dest = GetDisplayRectangle();
            //窗口在高 DPI 下会带入系统缩放，强制按物理像素 1:1 输出缓冲区
            g.ResetTransform();
            g.PageUnit = GraphicsUnit.Pixel;
            g.PageScale = 1f;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(bmp, dest);
        }

        //缓冲区显示区域：菜单栏以下整块铺满
        internal Rectangle GetDisplayRectangle()
        {
            int cw = ClientRectangle.Width;
            int ch = ClientRectangle.Height;
            int top = (menuStrip != null && menuStrip.Visible) ? menuStrip.Height : 0;
            int availH = ch - top;
            if (cw <= 0 || availH <= 0)
            {
                return new Rectangle(0, top, Math.Max(1, cw), Math.Max(1, availH));
            }
            return new Rectangle(0, top, cw, availH);
        }

        //将窗口鼠标坐标换算为逻辑坐标
        internal MouseEventArgs ToLogicalMouse(MouseEventArgs e)
        {
            Rectangle dest = GetDisplayRectangle();
            if (dest.Width <= 0 || dest.Height <= 0)
            {
                return e;
            }

            double sx = (double)CanvasWidth / dest.Width;
            double sy = (double)CanvasHeight / dest.Height;
            int x = (int)Math.Round((e.X - dest.X) * sx) - ContentOffsetX;
            int y = (int)Math.Round((e.Y - dest.Y) * sy) - ContentOffsetY;
            return new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta);
        }

        //背景铺满整个缓冲区（等比缩放后居中裁切），g 必须为未变换的像素坐标系
        internal void FillBackground(Graphics g)
        {
            if (image == null || bmp == null)
            {
                return;
            }

            GraphicsState state = g.Save();
            g.ResetTransform();
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            double s = Math.Max((double)bmp.Width / image.Width, (double)bmp.Height / image.Height);
            double dw = image.Width * s;
            double dh = image.Height * s;
            double ox = (bmp.Width - dw) / 2.0;
            double oy = (bmp.Height - dh) / 2.0;
            g.DrawImage(image, new RectangleF((float)ox, (float)oy, (float)dw, (float)dh));
            g.Restore(state);
        }

        //用背景还原某块逻辑区域，g 必须已应用 MarkRenderScaled
        //用背景还原某块逻辑区域，g 必须已应用 MarkRenderScaled
        internal void DrawBackgroundRegion(Graphics g, Rectangle r)
        {
            if (image == null || bmp == null)
            {
                return;
            }

            //与 FillBackground 使用同一映射：背景按 max 比例铺满缓冲区并居中
            double s = Math.Max((double)bmp.Width / image.Width, (double)bmp.Height / image.Height);
            double dw = image.Width * s;
            double dh = image.Height * s;
            double ox = (bmp.Width - dw) / 2.0;
            double oy = (bmp.Height - dh) / 2.0;

            double dx = (r.X + ContentOffsetX) * RenderScale;
            double dy = (r.Y + ContentOffsetY) * RenderScale;
            double dwidth = r.Width * RenderScale;
            double dheight = r.Height * RenderScale;

            double sx = (dx - ox) / s;
            double sy = (dy - oy) / s;
            double sw = dwidth / s;
            double sh = dheight / s;

            if (sx < 0) { sw += sx; sx = 0; }
            if (sy < 0) { sh += sy; sy = 0; }
            if (sx + sw > image.Width) { sw = image.Width - sx; }
            if (sy + sh > image.Height) { sh = image.Height - sy; }
            if (sw <= 0.5 || sh <= 0.5)
            {
                return;
            }

            g.DrawImage(image, new RectangleF(r.X, r.Y, r.Width, r.Height), new RectangleF((float)sx, (float)sy, (float)sw, (float)sh), GraphicsUnit.Pixel);
        }

        internal void MarkRenderScaled(Graphics g)
        {
            g.ScaleTransform(RenderScale, RenderScale);
            g.TranslateTransform(ContentOffsetX, ContentOffsetY);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        }

        private ArrayList GetHintCards()
        {
            ArrayList hint = new ArrayList();
            if (currentPokers == null || currentPokers.Length < 4 || currentPokers[0].Count == 0
                || currentSendCards == null || firstSend < 1 || firstSend > 4)
            {
                return hint;
            }

            CurrentPoker[] savedPokers = currentPokers;
            ArrayList[] savedPokerList = pokerList;
            ArrayList[] savedSendCards = currentSendCards;
            int savedWhoIsBigger = whoIsBigger;

            try
            {
                CurrentPoker[] pokerCopies = new CurrentPoker[4];
                ArrayList[] listCopies = new ArrayList[4];
                ArrayList[] sendCopies = new ArrayList[4];
                for (int i = 0; i < 4; i++)
                {
                    pokerCopies[i] = currentPokers[i].Clone();
                    listCopies[i] = new ArrayList(pokerList[i]);
                    sendCopies[i] = new ArrayList(currentSendCards[i]);
                }

                currentPokers = pokerCopies;
                pokerList = listCopies;
                currentSendCards = sendCopies;

                if (firstSend == 1)
                {
                    ShouldSendedCardsAlgorithm.ShouldSendCards(this, pokerCopies, 1, hint);
                }
                else
                {
                    MustSendCardsAlgorithm.MustSendCards(this, pokerCopies, 1, hint, sendCopies[firstSend - 1].Count);
                }
            }
            catch
            {
                hint.Clear();
            }
            finally
            {
                currentPokers = savedPokers;
                pokerList = savedPokerList;
                currentSendCards = savedSendCards;
                whoIsBigger = savedWhoIsBigger;
            }

            if (!IsHintValid(hint))
            {
                return FindSimpleHint();
            }

            return hint;
        }

        private ArrayList FindSimpleHint()
        {
            ArrayList result = new ArrayList();
            if (myCardsNumber == null || myCardsNumber.Count == 0)
            {
                return result;
            }

            int need = 1;
            if (firstSend != 1 && currentSendCards != null && firstSend > 0)
            {
                need = currentSendCards[firstSend - 1].Count;
            }

            for (int i = 0; i < myCardsNumber.Count; i++)
            {
                result.Clear();
                result.Add((int)myCardsNumber[i]);
                if (need == 1 && IsHintValid(result))
                {
                    return result;
                }
            }

            result.Clear();
            return result;
        }

        private bool IsHintValid(ArrayList hint)
        {
            if (hint == null || hint.Count == 0 || currentSendCards == null)
            {
                return false;
            }

            ArrayList[] test = new ArrayList[4];
            for (int i = 0; i < 4; i++)
            {
                test[i] = currentSendCards[i] == null
                    ? new ArrayList()
                    : new ArrayList(currentSendCards[i]);
            }

            return TractorRules.IsInvalid(this, test, hint, 1);
        }

        private void ToggleHint()
        {
            if (currentState.CurrentCardCommands != CardCommands.WaitingForMySending || whoseOrder != 1)
            {
                return;
            }

            if (hintActive)
            {
                RestoreHintSelection();
            }
            else
            {
                ArrayList hint = GetHintCards();
                if (hint.Count == 0)
                {
                    return;
                }

                hintPreviousReady = new ArrayList();
                for (int i = 0; i < myCardIsReady.Count; i++)
                {
                    hintPreviousReady.Add((bool)myCardIsReady[i]);
                    myCardIsReady[i] = false;
                }

                ArrayList remaining = new ArrayList(hint);
                for (int i = 0; i < myCardsNumber.Count && remaining.Count > 0; i++)
                {
                    for (int j = 0; j < remaining.Count; j++)
                    {
                        if ((int)myCardsNumber[i] == (int)remaining[j])
                        {
                            myCardIsReady[i] = true;
                            remaining.RemoveAt(j);
                            break;
                        }
                    }
                }

                if (!TractorRules.IsInvalid(this, currentSendCards, 1))
                {
                    RestoreHintSelection();
                    drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                    Refresh();
                    return;
                }

                hintActive = true;
            }

            drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
            Refresh();
        }

        private void RestoreHintSelection()
        {
            hintActive = false;
            if (hintPreviousReady.Count == myCardIsReady.Count)
            {
                for (int i = 0; i < myCardIsReady.Count; i++)
                {
                    myCardIsReady[i] = (bool)hintPreviousReady[i];
                }
            }
            hintPreviousReady.Clear();
        }

        private void CancelHintState()
        {
            hintActive = false;
            hintPreviousReady.Clear();
        }
        private void MainForm_MouseClick(object sender, MouseEventArgs e)
        {
            e = ToLogicalMouse(e);

            if (showingStartMenu)
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (startMenuStartRect.Contains(e.Location))
                    {
                        startMenuMessage = "";
                        MenuItem_Click(StartToolStripMenuItem, EventArgs.Empty);
                        return;
                    }

                    if (startMenuLoadRect.Contains(e.Location))
                    {
                        startMenuMessage = "";
                        RestoreToolStripMenuItem_Click(RestoreToolStripMenuItem, EventArgs.Empty);
                        return;
                    }

                    if (startMenuExitRect.Contains(e.Location))
                    {
                        Close();
                        return;
                    }
                }

                return;
            }

            //只有发牌时和该我出牌时才能响应鼠标事件
            if (((currentState.CurrentCardCommands == CardCommands.WaitingForMySending) || (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)) && (whoseOrder == 1))
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (DrawingFormHelper.HintButtonRect.Contains(e.X, e.Y))
                    {
                        ToggleHint();
                        return;
                    }

                    if (DrawingFormHelper.PlayButtonRect.Contains(e.X, e.Y))
                    {
                        PlaySelectedCards();
                        return;
                    }

                    if (hintActive)
                    {
                        CancelHintState();
                    }

                    if ((e.X >= (int)myCardsLocation[0] && e.X <= ((int)myCardsLocation[myCardsLocation.Count - 1] + 71)) && (e.Y >= DrawingFormHelper.HAND_Y_LIFTED && e.Y < (DrawingFormHelper.HAND_Y + 96)))
                    {
                        if (calculateRegionHelper.CalculateClickedRegion(e, 1))
                        {
                            drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                            Refresh();
                        }
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    if (hintActive)
                    {
                        CancelHintState();
                    }
                    int i = calculateRegionHelper.CalculateRightClickedRegion(e);
                    if (i > -1 && i < myCardIsReady.Count)
                    {
                        bool b = (bool)myCardIsReady[i];
                        int x = (int)myCardsLocation[i];
                        for (int j = 1; j <= i; j++)
                        {
                            if ((int)myCardsLocation[i - j] == (x - DrawingFormHelper.CARD_STEP))
                            {
                                myCardIsReady[i - j] = b;
                                x = x - DrawingFormHelper.CARD_STEP;
                            }
                            else
                            {
                                break;
                            }
                        }

                        drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                        Refresh();
                    }
                }


            }
            else if (currentState.CurrentCardCommands == CardCommands.ReadyCards)
            {
                drawingFormHelper.IsClickedRanked(e);
            }
        }
        private void PlaySelectedCards()
        {
            if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards)
            {
                ArrayList readyCards = new ArrayList();
                for (int i = 0; i < myCardIsReady.Count; i++)
                {
                    if ((bool)myCardIsReady[i])
                    {
                        readyCards.Add((int)myCardsNumber[i]);
                    }
                }

                if (readyCards.Count == 8)
                {
                    drawingFormHelper.HideActionButtons();
                    CancelHintState();
                    send8Cards = new ArrayList();
                    for (int i = 0; i < 8; i++)
                    {
                        CommonMethods.SendCards(send8Cards, currentPokers[0], pokerList[0], (int)readyCards[i]);
                    }
                    initSendedCards();
                    currentState.CurrentCardCommands = CardCommands.DrawMySortedCards;
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending)
            {
                if (TractorRules.IsInvalid(this, currentSendCards, 1))
                {
                    drawingFormHelper.HideActionButtons();
                    CancelHintState();

                    if (firstSend == 1)
                    {
                        whoIsBigger = 1;
                        ArrayList minCards = new ArrayList();
                        if (TractorRules.CheckSendCards(this, minCards, 0))
                        {
                            currentSendCards[0] = new ArrayList();
                            for (int i = 0; i < myCardIsReady.Count; i++)
                            {
                                if ((bool)myCardIsReady[i])
                                {
                                    CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < minCards.Count; i++)
                            {
                                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)minCards[i]);
                            }
                        }
                    }
                    else
                    {
                        currentSendCards[0] = new ArrayList();
                        for (int i = 0; i < myCardIsReady.Count; i++)
                        {
                            if ((bool)myCardIsReady[i])
                            {
                                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
                            }
                        }
                    }

                    drawingFormHelper.DrawMyFinishSendedCards();
                }
            }
        }
        private void MainForm_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            e = ToLogicalMouse(e);

            //if (e.Button == MouseButtons.Right)
            //    return;

            //如果当前没有牌可出 

            if (currentPokers[0].Count == 0)
            {
                return;
            }

            bool  b = calculateRegionHelper.CalculateDoubleClickedRegion(e);
            if (!b)
            {
                return;
            }

            currentSendCards[0]= new ArrayList();


            //出牌，所以擦去小猪
            drawingFormHelper.HideActionButtons();
            CancelHintState();
           
           

            //扣牌还是出牌
            if ((currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards) && (whoseOrder == 1)) //如果等我扣牌
            {
                ArrayList readyCards = new ArrayList();
                for (int i = 0; i < myCardIsReady.Count; i++)
                {
                    if ((bool)myCardIsReady[i])
                    {
                        readyCards.Add((int)myCardsNumber[i]);
                    }
                }

                if (readyCards.Count == 8)
                {
                    send8Cards = new ArrayList();
                    for (int i = 0; i < 8; i++)
                    {
                        CommonMethods.SendCards(send8Cards, currentPokers[0], pokerList[0], (int)readyCards[i]);
                    }
                    initSendedCards();
                    currentState.CurrentCardCommands = CardCommands.DrawMySortedCards;
                }


            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingForMySending) //如果等我发牌
            {
               

                if (TractorRules.IsInvalid(this, currentSendCards, 1))
                {
                    if (firstSend == 1)
                    {
                        whoIsBigger = 1;

                        ArrayList minCards = new ArrayList();
                        if (TractorRules.CheckSendCards(this, minCards,0))
                        {
                            currentSendCards[0] = new ArrayList(); 
                            for (int i = 0; i < myCardIsReady.Count; i++)
                            {
                                if ((bool)myCardIsReady[i])
                                {
                                    CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
                                }
                            }

                        }
                        else
                        {
                            for (int i = 0; i < minCards.Count; i++)
                            {
                                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)minCards[i]);
                            }
                           
                        }


                    }
                    else
                    {
                        currentSendCards[0] = new ArrayList(); 
                        for (int i = 0; i < myCardIsReady.Count; i++)
                        {
                            if ((bool)myCardIsReady[i])
                            {
                                CommonMethods.SendCards(currentSendCards[0], currentPokers[0], pokerList[0], (int)myCardsNumber[i]);
                            }
                        }
                    }


                    drawingFormHelper.DrawMyFinishSendedCards();
                }
            }


        }

        //初始化每个人出的牌
        internal void initSendedCards()
        {
            //重新解析每个人手中的牌
            currentPokers[0] = CommonMethods.parse(pokerList[0], currentState.Suit, currentRank);
            currentPokers[1] = CommonMethods.parse(pokerList[1], currentState.Suit, currentRank);
            currentPokers[2] = CommonMethods.parse(pokerList[2], currentState.Suit, currentRank);
            currentPokers[3] = CommonMethods.parse(pokerList[3], currentState.Suit, currentRank);
        }


        #endregion // 窗口事件处理程序


        //定时器,用来显示发牌时的动画
        internal void timer_Tick(object sender, EventArgs e)
        {

            if (musicFile.Length > 0 && (!MciSoundPlayer.IsPlaying()) && PlayMusicToolStripMenuItem.Checked)
            {
                MciSoundPlayer.Stop();
                MciSoundPlayer.Close();
                MciSoundPlayer.Play(musicFile,"song");
            }
            else if (musicFile.Length > 0 && (!MciSoundPlayer.IsPlaying()) && RandomPlayToolStripMenuItem.Checked)
            {
                PlayRandomSongs();
            }
            //1.分牌
            if (currentState.CurrentCardCommands == CardCommands.ReadyCards) //分牌
            {
                if (currentCount ==0)
                {
                    //画工具栏
                    if (!gameConfig.IsDebug)
                    {
                        drawingFormHelper.DrawToolbar();
                    }

                }

                if (currentCount < 25)
                {
                    drawingFormHelper.ReadyCards(currentCount);
                    currentCount++;
                    
                }
                else
                {
                    currentState.CurrentCardCommands = CardCommands.DrawCenter8Cards;
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingShowBottom) //翻底牌完毕后的清理工作
            {
                drawingFormHelper.DrawCenterImage();
                //画8张牌的背面
                Graphics g = Graphics.FromImage(bmp);
                MarkRenderScaled(g);

                for (int i = 0; i < 8; i++)
                {
                    g.DrawImage(gameConfig.BackImage, 200 + i * 2, 186, 71, 96);
                }

                SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawCenter8Cards);

            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawCenter8Cards) //2.抓底牌
            {

                //如果无人亮主，流局
                if (drawingFormHelper.DoRankNot())
                {
                    if (gameConfig.IsPass) //如果设置为流局
                    {
                        //暂停3秒
                        init();
                        isNew = false;
                        //画图片
                        drawingFormHelper.DrawPassImage();
                        SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowPass);
                        return;
                    }
                    else //如果设置为翻底牌
                    {
                        //将底牌的第三张的花色设置为主
                        ArrayList bottom = new ArrayList();
                        bottom.Add(pokerList[0][0]);
                        bottom.Add(pokerList[0][1]);
                        bottom.Add(pokerList[1][0]);
                        bottom.Add(pokerList[1][1]);
                        bottom.Add(pokerList[2][0]);
                        bottom.Add(pokerList[2][1]);
                        bottom.Add(pokerList[3][0]);
                        bottom.Add(pokerList[3][1]);
                        int suit = CommonMethods.GetSuit((int)bottom[2]);
                        currentState.Suit = suit;

                Graphics g = Graphics.FromImage(bmp);
                MarkRenderScaled(g);

                        if (currentState.Master == 1 || currentState.Master == 2)
                        {
                            drawingFormHelper.DrawSuit(g, suit, true, true);
                        }
                        else if (currentState.Master == 3 || currentState.Master == 4)
                        {
                            drawingFormHelper.DrawSuit(g, suit, false, true);
                        }

                        g.Dispose();

                        //在中央画8张底牌,第三张稍微向上
                        drawingFormHelper.DrawCenterImage();
                        drawingFormHelper.DrawBottomCards(bottom);

                        //暂停一段时间,让大家能看到翻的底牌
                        SetPauseSet(gameConfig.NoRankPauseTime, CardCommands.WaitingShowBottom);

                        return;
                    }
                }


                whoseOrder = currentState.Master;//第一次由主家发牌
                firstSend = whoseOrder;

                SetPauseSet(gameConfig.Get8CardsTime, CardCommands.DrawMySortedCards);

                drawingFormHelper.DrawCenter8Cards();

                initSendedCards();
                drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
                currentState.CurrentCardCommands = CardCommands.WaitingForSending8Cards; //摸牌完毕，排序我的牌
                drawingFormHelper.DrawActionButtons();
                Refresh();

                //初始化得分牌
                drawingFormHelper.DrawScoreImage(0);


            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingShowPass) //显示流局信息
            {
                //将流局图片清理掉
                drawingFormHelper.DrawCenterImage();
                //drawingFormHelper.DrawScoreImage(0);
                Refresh();
                currentState.CurrentCardCommands = CardCommands.ReadyCards;
            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards) //3.扣底牌
            {


                //如果需要
                switch (currentState.Master)
                {
                    case 1:
                        if (gameConfig.IsDebug)
                        {
                            Algorithm.Send8Cards(this, 1);
                        }
                        else
                        {
                            drawingFormHelper.DrawMyPlayingCards(currentPokers[0]);
                            Refresh();
                            return;
                        }
                        break;
                    case 2:
                        Algorithm.Send8Cards(this, 2);
                        break;
                    case 3:
                        Algorithm.Send8Cards(this, 3);
                        break;
                    case 4:
                        Algorithm.Send8Cards(this, 4);
                        break;
                }



            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawMySortedCards) //4.画我的牌
            {

                //将最后自己的牌进行排序显示
                SetPauseSet(gameConfig.SortCardsTime, CardCommands.DrawMySortedCards);

                drawingFormHelper.DrawMySortedCards(currentPokers[0], currentPokers[0].Count);
                Refresh();

                currentState.CurrentCardCommands = CardCommands.WaitingForSend;

            }
            else if (currentState.CurrentCardCommands == CardCommands.WaitingForSend) //等待出牌
            {
                //如果是对家
                if (whoseOrder == 2)
                {
                    drawingFormHelper.DrawFrieldUserSendedCards();
                }
                if (whoseOrder == 3)
                {
                    drawingFormHelper.DrawPreviousUserSendedCards();
                }
                if (whoseOrder == 4)
                {
                    drawingFormHelper.DrawNextUserSendedCards();
                }
                if (whoseOrder == 1)
                {
                    if (gameConfig.IsDebug)
                    {
                        if (firstSend == 1)
                        {
                            Algorithm.ShouldSendedCards(this, 1, currentPokers, currentSendCards, currentState.Suit, currentRank);
                        }
                        else
                        {
                            Algorithm.MustSendedCards(this, 1, currentPokers, currentSendCards, currentState.Suit, currentRank, currentSendCards[firstSend - 1].Count);
                        }
                        drawingFormHelper.DrawMyFinishSendedCards();
                        if (currentSendCards[3].Count > 0) //是否完成
                        {
                            currentState.CurrentCardCommands = CardCommands.Pause;
                            SetPauseSet(gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);
                        }
                        else
                        {
                            whoseOrder = 4;
                            currentState.CurrentCardCommands = CardCommands.WaitingForSend;
                        }
                    }
                    else
                    {
                        currentState.CurrentCardCommands = CardCommands.WaitingForMySending;//等待鼠标事件
                        drawingFormHelper.DrawActionButtons();
                        Refresh();
                    }
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.Pause) //如果需要暂停
            {
                //如果是Pause,则只是让程序休息一会()
                long interval = (DateTime.Now.Ticks - sleepTime) / 10000;

                if (interval > sleepMaxTime)
                {
                    currentState.CurrentCardCommands = wakeupCardCommands;
                }


            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawOnceFinished) //如果是大家都出完牌
            {
                drawingFormHelper.DrawFinishedOnceSendedCards(); //完成清理工作
                if (currentPokers[0].Count > 0)
                {
                    currentState.CurrentCardCommands = CardCommands.WaitingForSend;
                }
            }
            else if (currentState.CurrentCardCommands == CardCommands.DrawOnceRank) //如果本轮大家都出完牌
            {
                currentState.CurrentCardCommands = CardCommands.Undefined;
                init();
            }
        }

        //设置暂停的最大时间，以及暂停结束后的执行命令
        internal void SetPauseSet(int max, CardCommands wakeup)
        {
            sleepMaxTime = max;
            sleepTime = DateTime.Now.Ticks;
            wakeupCardCommands = wakeup;
            currentState.CurrentCardCommands = CardCommands.Pause;
        }


        #region 菜单事件处理
        //牌面图案
        private void SelectCardImage_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Text.Equals("普通图案"))
            {
                gameConfig.CardsResourceManager = Kuaff_Cards.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Checked;
                ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "自定义";
                gameConfig.CardImageName = "";

            }
            else if (menuItem.Text.Equals("香车美女"))
            {
                gameConfig.CardsResourceManager = Kuaff_Model.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                ModelToolStripMenuItem.CheckState = CheckState.Checked;
                OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "自定义";
                gameConfig.CardImageName = "";
            }
            else if (menuItem.Text.Equals("京剧脸谱"))
            {
                gameConfig.CardsResourceManager = Kuaff_Opera.ResourceManager;
                CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                OperaToolStripMenuItem.CheckState = CheckState.Checked;
                CustomCardImageToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomCardImageToolStripMenuItem.Text = "自定义";
                gameConfig.CardImageName = "";
            }
            else if (menuItem.Text.StartsWith("自定义"))
            {
                SelectCardsImage sci = new SelectCardsImage(this);
                if (sci.ShowDialog(this) == DialogResult.OK)
                {
                    gameConfig.CardImageName = sci.CardsName;
                    menuItem.Text = "自定义--" + gameConfig.CardImageName;

                    CommonToolStripMenuItem.CheckState = CheckState.Unchecked;
                    ModelToolStripMenuItem.CheckState = CheckState.Unchecked;
                    OperaToolStripMenuItem.CheckState = CheckState.Unchecked;
                    CustomCardImageToolStripMenuItem.CheckState = CheckState.Checked;
                }
            }
        }
        //牌背图片
        private void SelectBackImage_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.Text.Equals("蔚蓝世界"))
            {
                gameConfig.BackImage = Kuaff_Cards.back;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Checked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "自定义";
            }
            else if (menuItem.Text.Equals("青涩年华"))
            {
                gameConfig.BackImage = Kuaff_Cards.back2;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Checked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "自定义";
            }
            else if (menuItem.Text.Equals("草原羚羊"))
            {
                gameConfig.BackImage = Kuaff_Cards.back3;
                BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                AntelopeToolStripMenuItem.CheckState = CheckState.Checked;

                CustomBackImageToolStripMenuItem.CheckState = CheckState.Unchecked;
                CustomBackImageToolStripMenuItem.Text = "自定义";
            }
            else if (menuItem.Text.StartsWith("自定义"))
            {
                SelectCardbackImage sci = new SelectCardbackImage(this);
                if (sci.ShowDialog(this) == DialogResult.OK)
                {
                    menuItem.Text = "自定义--" + sci.CardBackImageName;

                    BlueWorldToolStripMenuItem.CheckState = CheckState.Unchecked;
                    GreenAgeToolStripMenuItem.CheckState = CheckState.Unchecked;
                    AntelopeToolStripMenuItem.CheckState = CheckState.Unchecked;
                    CustomBackImageToolStripMenuItem.CheckState = CheckState.Checked;
                }

            }
        }

        //选择背景图片
        //选择背景图片
        private void SelectImage_Click(object sender, EventArgs e)
        {
            PauseGametoolStripMenuItem.Text = "暂停游戏";

            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("夸父科技"))
            {
                KuaffToolStripMenuItem.CheckState = CheckState.Checked;
                image = global::Kuaff.Tractor.Properties.Resources.Backgroud;
                init();
                Invalidate();
            }
            else if (menuItem.Text.Equals("自定义图片"))
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    KuaffToolStripMenuItem.CheckState = CheckState.Unchecked;
                    image = new Bitmap(openFileDialog.OpenFile());
                    init();
                    Invalidate();
                }
            }
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            this.Activate();
        }

        //全屏状态
        internal bool isFullScreen = false;

        //切换全屏显示
        private void FullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && isFullScreen)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        internal void ToggleFullScreen()
        {
            if (isFullScreen)
            {
                isFullScreen = false;
                menuStrip.Visible = true;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                WindowState = FormWindowState.Normal;
                ClientSize = new Size(629, 470);
                MaximizeBox = true;
                FullScreenToolStripMenuItem.Checked = false;
            }
            else
            {
                isFullScreen = true;
                //全屏时保留菜单栏，便于随时切换或退出
                menuStrip.Visible = true;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                FullScreenToolStripMenuItem.Checked = true;
            }

            ApplyDisplayMode();
        }

        //进入/退出全屏后同步缓冲区和重绘
        //进入/退出全屏后同步缓冲区和重绘
        private void ApplyDisplayMode()
        {
            if (bmp == null)
            {
                return;
            }

            if (IsHandleCreated)
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    ReflowCanvas();
                    Invalidate();
                });
            }
            else
            {
                ReflowCanvas();
                Invalidate();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
                notifyIcon.Visible = true;
                return;
            }

            notifyIcon.Visible = false;

            //窗口比例变化（如最大化）后重建画布，避免拉伸变形或出现黑边
            if (bmp != null && ClientRectangle.Width > 0 && ClientRectangle.Height > 0)
            {
                int top = (menuStrip != null && menuStrip.Visible) ? menuStrip.Height : 0;
                int availH = ClientRectangle.Height - top;
                if (availH > 0)
                {
                    double want = (double)ClientRectangle.Width / availH;
                    double have = (double)bmp.Width / bmp.Height;
                    if (Math.Abs(want - have) > 0.01)
                    {
                        ReflowCanvas();
                    }
                }
            }
        }

        //按当前窗口比例重排画面（不重新发牌）
        internal void ReflowCanvas()
        {
            if (bmp == null)
            {
                return;
            }

            int oldOffsetX = ContentOffsetX;
            int oldOffsetY = ContentOffsetY;
            Rectangle oldContentRect = new Rectangle(
                oldOffsetX * RenderScale,
                oldOffsetY * RenderScale,
                LogicalWidth * RenderScale,
                LogicalHeight * RenderScale);

            using (Bitmap previousContent = bmp.Clone(oldContentRect, bmp.PixelFormat))
            {
                PrepareRenderBuffer();

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    drawingFormHelper.DrawBackground(g);
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;

                    Rectangle newContentRect = new Rectangle(
                        ContentOffsetX * RenderScale,
                        ContentOffsetY * RenderScale,
                        previousContent.Width,
                        previousContent.Height);
                    g.DrawImage(
                        previousContent,
                        newContentRect,
                        0,
                        0,
                        previousContent.Width,
                        previousContent.Height,
                        GraphicsUnit.Pixel);
                }
            }

            Invalidate();
        }

        private void GameSpeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetSpeedDialog dialog = new SetSpeedDialog(this);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                //调整速度
                gameConfig.FinishedOncePauseTime = (int)(150 * Math.Pow(10, dialog.trackBar1.Value / 25.0));
                gameConfig.NoRankPauseTime = (int)(500 * Math.Pow(10, dialog.trackBar2.Value / 25.0));
                gameConfig.Get8CardsTime = (int)(100 * Math.Pow(10, dialog.trackBar3.Value / 25.0));
                gameConfig.SortCardsTime = (int)(100 * Math.Pow(10, dialog.trackBar4.Value / 25.0));
                gameConfig.FinishedThisTime = (int)(250 * Math.Pow(10, dialog.trackBar5.Value / 25.0));
                gameConfig.TimerDiDa = (int)(10 * Math.Pow(10, dialog.trackBar6.Value / 25.0));
                timer.Interval = gameConfig.TimerDiDa;
            }
        }

        //保存牌局
        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stream stream = null;
            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream("backup", FileMode.Create, FileAccess.Write, FileShare.None);
                formatter.Serialize(stream, currentState);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        //读取牌局
        private void RestoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PauseGametoolStripMenuItem.Text = "暂停游戏";

            Stream stream = null;
            try
            {
                IFormatter formatter = new BinaryFormatter();
                stream = new FileStream("backup", FileMode.Open, FileAccess.Read, FileShare.Read);
                CurrentState cs = (CurrentState)formatter.Deserialize(stream);
                
                currentState = cs;

               
                if (currentState.Master == 1 || currentState.Master == 2)
                {
                    currentRank = currentState.OurCurrentRank;
                }
                else if(currentState.Master == 3 || currentState.Master == 4)
                {
                    currentRank = currentState.OpposedCurrentRank;
                }
                else
                {
                    isNew = true;
                    currentRank = 0;
                }

                init();

                timer.Start();
                showingStartMenu = false;
                startMenuMessage = "";
            }
            catch (Exception ex)
            {
                if (showingStartMenu)
                {
                    startMenuMessage = File.Exists("backup") ? "牌局读取失败，请检查存档" : "暂无可读取的牌局";
                    ShowStartMenu();
                }
                else
                {
                    MessageBox.Show(this, "没有找到可读取的牌局，或存档已经损坏。", "读取牌局", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }
        //显示帮助
        private void GameHelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this,"Tractor.CHM");
        }

        //Aboutme
        private void AboutMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show(this);
        }

        private void PauseGametoolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            if (menuItem.Text.Equals("暂停游戏"))
            {
                timer.Stop();
                menuItem.Text = "继续游戏";
                menuItem.Image = Properties.Resources.MenuResume;
            }
            else
            {
                timer.Start();
                menuItem.Text = "暂停游戏";
                menuItem.Image = Properties.Resources.MenuPause;
            }
        }

        private void RobotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;

            if (menuItem.CheckState == CheckState.Checked)
            {
                gameConfig.IsDebug = true;
            }
            else
            {
                gameConfig.IsDebug = false;
            }
        }

        private void SetRulesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRules sr = new SetRules(this);
            sr.ShowDialog(this);
        }

        private void NoBackMusicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            menuItem.CheckState = CheckState.Checked;
            
        }

        private void PlayMusicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //弹出内置音乐选择对话框
            SelectMusic sem = new SelectMusic();
            if (sem.ShowDialog(this) == DialogResult.OK)
            {
                NoBackMusicToolStripMenuItem.CheckState = CheckState.Unchecked;

                //如果选择了一首曲子，则播放
                try
                {
                    string music = (string)sem.music.SelectedItem;
                    String newMusicFile = Path.Combine(Application.StartupPath, "music\\" + music);
                    if (musicFile != newMusicFile && musicFile.Length > 0)
                    {
                        MciSoundPlayer.Stop();
                        MciSoundPlayer.Close();
                    }
                    musicFile = newMusicFile;
                    MciSoundPlayer.Play(musicFile,"song");

                    NoBackMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
                    PlayMusicToolStripMenuItem.CheckState = CheckState.Checked;
                    RandomPlayToolStripMenuItem.CheckState = CheckState.Unchecked;
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                //NoBackMusicToolStripMenuItem.CheckState = CheckState.Checked;

            }
        }

        private void NoBackMusicToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            musicFile = "";
            MciSoundPlayer.Stop();
            MciSoundPlayer.Close();
            NoBackMusicToolStripMenuItem.CheckState = CheckState.Checked;
            RandomPlayToolStripMenuItem.CheckState = CheckState.Unchecked;
            PlayMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
        }

        private void RandomPlayToolStripMenuItem_Click(object sender, EventArgs e)
        {

            PlayRandomSongs();

            NoBackMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
            RandomPlayToolStripMenuItem.CheckState = CheckState.Checked;
            PlayMusicToolStripMenuItem.CheckState = CheckState.Unchecked;
        }

        //随机播放音乐
        private void PlayRandomSongs()
        {
            try
            {
                SelectMusic sem = new SelectMusic();
                int count = sem.music.Items.Count;
                Random random = new Random();
                string music = (string)sem.music.Items[random.Next(count)];
                sem.Dispose();
                String newMusicFile = Path.Combine(Application.StartupPath, "music\\" + music);
                if (musicFile != newMusicFile && musicFile.Length > 0)
                {
                    MciSoundPlayer.Stop();
                    MciSoundPlayer.Close();
                }
                musicFile = newMusicFile;
                MciSoundPlayer.Play(musicFile, "song");

            }
            catch (Exception ex)
            {

            }
        }

        private void FereToolStripMenuItem_Click(object sender, EventArgs e) //拖拉机伴侣
        {
            Fere fere = new Fere();
            fere.Show(this);
        }

        private void SeeTotalScoresToolStripMenuItem_Click(object sender, EventArgs e) //得分统计
        {
            TotalScores ts = new TotalScores(this);
            ts.Show(this);
        }

        private void SelectAlgorithmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectUserAlgorithm sua = new SelectUserAlgorithm(this);
            sua.ShowDialog(this);
        }

        #endregion // 菜单事件处理

        private void SetGameFinishedtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetGameFinished sgf = new SetGameFinished(this);
            sgf.ShowDialog(this);
        }

        

               
    }
}
