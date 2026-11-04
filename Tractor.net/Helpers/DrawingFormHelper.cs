using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using Kuaff.CardResouces;

namespace Kuaff.Tractor
{
    /// <summary>
    /// 实现大部分的绘画操作
    /// </summary>
    class DrawingFormHelper
    {
        MainForm mainForm;
        internal DrawingFormHelper(MainForm mainForm)
        {
            this.mainForm = mainForm;
        }

      
        #region 发牌动画

        /// <summary>
        /// 准备发牌.
        /// 首先在程序中央画好58-i*2张牌(实际25+8就可以了，为了显示牌多，这里用50+8),
        /// 每发一次牌，减少两张。
        /// 
        /// 然后，每个人手中发一次牌，然后从我开始，依次画得到牌后的界面。
        /// 其他三个人画完牌后，应该调用算法中的方法，判断是否应该亮主。
        /// </summary>
        //牌与牌之间的水平间距（逻辑像素）；手牌多于 27 张时会自动压缩
        internal static int CARD_STEP = 20;

        //按手牌数量计算合适的步长（33 张收底时可缩到 17，仍能露出角标）
        internal static int CalcCardStep(int index)
        {
            if (index <= 1) { return 20; }
            int maxW = MainForm.LogicalWidth - 6 - 71;
            int step = maxW / (index - 1);
            if (step > 20) { step = 20; }
            if (step < 11) { step = 11; }
            return step;
        }

        //出牌区牌的横向间距（比手牌大，保证角标数字完整可见）
        internal const int PLAYED_STEP = 22;

        //手牌纵向位置（上移，避免底边被裁）
        internal const int HAND_Y = 369;
        internal const int HAND_Y_LIFTED = 354;

        // Action buttons
        internal static readonly Rectangle PlayButtonRect = new Rectangle(264, 300, 54, 30);
        internal static readonly Rectangle HintButtonRect = new Rectangle(326, 300, 54, 30);
        private static readonly Rectangle ActionButtonArea = new Rectangle(258, 294, 128, 42);

        //侧栏底板：左右各 88x116
        internal const int SIDEBAR_LEFT_X = 12;
        internal const int SIDEBAR_RIGHT_X = 529;
        internal const int SIDEBAR_Y = 30;
        internal const int SIDEBAR_W = 88;
        internal const int SIDEBAR_H = 116;

        //方位字（Master.png 每格 112px）
        internal const int MASTER_CELL = 112;
        internal const int MASTER_SIZE = 30;
        internal const int MASTER_Y = 38;
        internal const int MASTER_ME_X = 539;
        internal const int MASTER_OTHER_X = 577;
        internal const int MASTER_WEST_X = 22;
        internal const int MASTER_EAST_X = 60;

        //侧栏数字/花色（源图每格 160px）
        internal const int SIDEBAR_CELL = 160;
        internal const int SIDEBAR_RANK_Y = 70;
        internal const int SIDEBAR_SUIT_Y = 106;
        internal const int SIDEBAR_ICON_SIZE = 32;
        internal const int SIDEBAR_ME_X = 557;
        internal const int SIDEBAR_OTHER_X = 40;
        internal const int SUIT_DARK_INDEX = 5;

        //出牌指示器（Winner.png 152x244 = 4 倍 38x61）
        internal const int WINNER_W = 32;
        internal const int WINNER_H = 50;

        //指示器位置：贴近各家出牌，且全部落在出牌区清屏范围内（避免残留）
        internal const int WINNER_ME_X = 437;
        internal const int WINNER_ME_Y = 292;
        internal const int WINNER_OTHER_X = 437;
        internal const int WINNER_OTHER_Y = 152;
        internal const int WINNER_WEST_X = 106;
        internal const int WINNER_WEST_Y = 212;
        internal const int WINNER_EAST_X = 486;
        internal const int WINNER_EAST_Y = 212;

        //出牌区矩形（清屏与重绘共用）
        //主体：上沿从 y=148 开始（侧栏底边在 y=146），左右避开侧栏
        internal const int SEND_AREA_X = 77;
        internal const int SEND_AREA_Y = 148;
        internal const int SEND_AREA_W = 478;
        internal const int SEND_AREA_H = 220;

        //对家出牌行在 y=124 起，单独一块，避开左右侧栏
        internal const int SEND_NORTH_X = 103;
        internal const int SEND_NORTH_Y = 122;
        internal const int SEND_NORTH_W = 424;
        internal const int SEND_NORTH_H = 30;

        /// <param name="count">发牌次数，一共发25张牌，每人25张，最后庄家收底</param>
        internal void ReadyCards(int count)
        {

            //得到缓冲区图像的Graphics
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            //画牌局的中央，洗好的牌，实际画58张,每出一轮减少两张
            DrawCenterAllCards(g, 58 - count * 2);

            //当前每个人手中的牌
            mainForm.currentPokers[0].AddCard((int)mainForm.pokerList[0][count]);
            mainForm.currentPokers[1].AddCard((int)mainForm.pokerList[1][count]);
            mainForm.currentPokers[2].AddCard((int)mainForm.pokerList[2][count]);
            mainForm.currentPokers[3].AddCard((int)mainForm.pokerList[3][count]);

            //画自己的位置
            DrawAnimatedCard(getPokerImageByNumber((int)mainForm.pokerList[0][count]), 260, 280, 71, 96);
            DrawMyCards(g, mainForm.currentPokers[0], count);
            //判断我是否可以亮主
            if (mainForm.gameConfig.IsDebug)
            {
                DoRankOrNot(mainForm.currentPokers[0], 1);
            }
            else
            {

                MyRankOrNot(mainForm.currentPokers[0]);
            }
            mainForm.Refresh();

            //画对家的位置
            DrawAnimatedCard(mainForm.gameConfig.BackImage, 400 - count * 13, 60, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 437 - count * 13, 25, 71, 96);
            mainForm.Refresh();

            //是否亮主
            DoRankOrNot(mainForm.currentPokers[1], 2);

            //画西家的位置
            DrawAnimatedCard(mainForm.gameConfig.BackImage, 50, 160 + count * 4, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 6, 157 + count * 4, 71, 96);
            mainForm.Refresh();

            //是否亮主
            DoRankOrNot(mainForm.currentPokers[2], 3);

            //画东家的位置
            DrawAnimatedCard(mainForm.gameConfig.BackImage, 520, 220 - count * 4, 71, 96);
            DrawMyImage(g, mainForm.gameConfig.BackImage, 554, 253 - count * 4, 71, 96);
            mainForm.Refresh();


            //画亮的牌
            DrawSuitCards(g);
            //是否亮主
            DoRankOrNot(mainForm.currentPokers[3], 4);

            mainForm.Refresh();

            g.Dispose();
        }

        private void DrawSuitCards(Graphics g)
        {


            if (mainForm.showSuits == 1)
            {
                if (mainForm.whoShowRank == 2)
                {
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 437, 124, 71, 96);
                }
                else if (mainForm.whoShowRank == 3)
                {

                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 80, 158, 71, 96);
                }
                else if (mainForm.whoShowRank == 4)
                {
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 480, 200, 71, 96);
                }
            }
            else if (mainForm.showSuits == 2)
            {
                if (mainForm.whoShowRank == 2)
                {
                    ClearSuitCards(g);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 423, 124, 71, 96);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 437, 124, 71, 96);
                }
                else if (mainForm.whoShowRank == 3)
                {
                    ClearSuitCards(g);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 80, 158, 71, 96);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 80, 178, 71, 96);

                }
                else if (mainForm.whoShowRank == 4)
                {
                    ClearSuitCards(g);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 480, 200, 71, 96);
                    g.DrawImage(getPokerImageByNumber(mainForm.currentState.Suit * 13 - 13 + mainForm.currentRank), 480, 220, 71, 96);
                }
            }
        }
        //清除亮的牌
        private void ClearSuitCards(Graphics g)
        {
            mainForm.DrawBackgroundRegion(g, new Rectangle(423, 124, 85, 96));
            mainForm.DrawBackgroundRegion(g, new Rectangle(80, 158, 71, 116));
            mainForm.DrawBackgroundRegion(g, new Rectangle(480, 200, 71, 116));
        }

        #endregion // 发牌动画

        #region 画中心位置的牌
        /// <summary>
        /// 发牌时画中央的牌.
        /// 首先从底图中取相应的位置，重画这块背景。
        /// 然后用牌的背面画58-count*2张牌。
        /// 
        /// </summary>
        /// <param name="g">缓冲区图片的Graphics</param>
        /// <param name="num">牌的数量=58-发牌次数*2</param>
        internal void DrawCenterAllCards(Graphics g, int num)
        {
            mainForm.DrawBackgroundRegion(g, new Rectangle(200, 186, 188, 96));

            for (int i = 0; i < num; i++)
            {
                g.DrawImage(mainForm.gameConfig.BackImage, 200 + i * 2, 186, 71, 96);
            }
        }

        /// <summary>
        /// 发完一次牌，需要清理程序中心
        /// </summary>
        internal void DrawCenterImage()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            mainForm.DrawBackgroundRegion(g, new Rectangle(SEND_AREA_X, SEND_AREA_Y, SEND_AREA_W, SEND_AREA_H));
            mainForm.DrawBackgroundRegion(g, new Rectangle(SEND_NORTH_X, SEND_NORTH_Y, SEND_NORTH_W, SEND_NORTH_H));
            g.Dispose();
        }

        /// <summary>
        /// 画流局图片
        /// </summary>
        internal void DrawPassImage()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            Rectangle rect = new Rectangle(110, 150, 400, 199);
            g.DrawImage(Properties.Resources.Pass, rect);
            g.Dispose();
            mainForm.Refresh();
        }
        #endregion // 画中心位置的牌

        #region 底牌处理
        //收底牌的动画
        /// <summary>
        /// 发牌25次后，最后剩余8张牌.
        /// 这时已经确定了庄家，将8张牌交给庄家,
        /// 同时以动画的方式显示。
        /// </summary>
        internal void DrawCenter8Cards()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            Rectangle rect = new Rectangle(200, 186, 90, 96);
            Rectangle backRect = new Rectangle(194, 138, 214, 148);
            //最后8张的图像取出来
            Bitmap backup = mainForm.bmp.Clone(mainForm.LogicalToDevice(rect), PixelFormat.DontCare);
            //g.DrawImage(mainForm.image, rect, rect, GraphicsUnit.Pixel);
             mainForm.DrawBackgroundRegion(g, backRect);

            //将底牌8张交给庄家（直接交牌，避免动画在超采样缓冲中残留）
            backup.Dispose();
            if (mainForm.currentState.Master == 1)
            {
                Get8Cards(mainForm.pokerList[0], mainForm.pokerList[1], mainForm.pokerList[2], mainForm.pokerList[3]);
            }
            else if (mainForm.currentState.Master == 2)
            {
                Get8Cards(mainForm.pokerList[1], mainForm.pokerList[0], mainForm.pokerList[2], mainForm.pokerList[3]);
            }
            else if (mainForm.currentState.Master == 3)
            {
                Get8Cards(mainForm.pokerList[2], mainForm.pokerList[1], mainForm.pokerList[0], mainForm.pokerList[3]);
            }
            else if (mainForm.currentState.Master == 4)
            {
                Get8Cards(mainForm.pokerList[3], mainForm.pokerList[1], mainForm.pokerList[2], mainForm.pokerList[0]);
            }
            mainForm.Refresh();

            g.Dispose();
        }
        //将最后8张交给庄家
        private void Get8Cards(ArrayList list0, ArrayList list1, ArrayList list2, ArrayList list3)
        {
            list0.Add(list1[25]);
            list0.Add(list1[26]);
            list0.Add(list2[25]);
            list0.Add(list2[26]);
            list0.Add(list3[25]);
            list0.Add(list3[26]);
            list1.RemoveAt(26);
            list1.RemoveAt(25);
            list2.RemoveAt(26);
            list2.RemoveAt(25);
            list3.RemoveAt(26);
            list3.RemoveAt(25);
        }

        internal void DrawBottomCards(ArrayList bottom)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

            //画底牌,从169开始画
            for (int i = 0; i < 8; i++)
            {
                if (i ==2)
                {
                    g.DrawImage(getPokerImageByNumber((int)bottom[i]), 230 + i * 14, 146, 71, 96);
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber((int)bottom[i]), 230 + i * 14, 186, 71, 96);
                }
            }
           
            mainForm.Refresh();

            g.Dispose();
        }
        #endregion // 底牌处理


        #region 绘制Sidebar和toolbar
        /// <summary>
        /// 绘制Sidebar
        /// </summary>
        /// <param name="g"></param>
        internal void DrawSidebar(Graphics g)
        {
            //左右各一块 88x116 的木质底板
            DrawMyImage(g, Properties.Resources.Sidebar, SIDEBAR_LEFT_X, SIDEBAR_Y, SIDEBAR_W, SIDEBAR_H);
            DrawMyImage(g, Properties.Resources.Sidebar, SIDEBAR_RIGHT_X, SIDEBAR_Y, SIDEBAR_W, SIDEBAR_H);
        }

        internal void DrawMaster(Graphics g, int who, int start)
        {
            if (who < 1 || who > 4)
            {
                return;
            }

            //1=我(南) 2=对家(北) 3=西家 4=东家；start: 0=绿色常态, 1=红色庄家
            int cell = 0;
            int X = 0;
            if (who == 1) { cell = 2; X = MASTER_ME_X; }
            else if (who == 2) { cell = 3; X = MASTER_OTHER_X; }
            else if (who == 3) { cell = 0; X = MASTER_WEST_X; }
            else if (who == 4) { cell = 1; X = MASTER_EAST_X; }

            int srcX = (start == 0 ? cell : cell + 4) * MASTER_CELL;
            Rectangle destRect = new Rectangle(X, MASTER_Y, MASTER_SIZE, MASTER_SIZE);
            g.DrawImage(Properties.Resources.Master, destRect, new Rectangle(srcX, 0, MASTER_CELL, MASTER_CELL), GraphicsUnit.Pixel);
        }


        //画单个绿色的方位字
        private void DrawMasterCell(Graphics g, int who, int cell, int X)
        {
            if (who == mainForm.currentState.Master)
            {
                return;
            }
            g.DrawImage(Properties.Resources.Master,
                new Rectangle(X, MASTER_Y, MASTER_SIZE, MASTER_SIZE),
                new Rectangle(cell * MASTER_CELL, 0, MASTER_CELL, MASTER_CELL),
                GraphicsUnit.Pixel);
        }

        /// <summary>
        /// </summary>
        /// <param name="g"></param>
        /// <param name="who"></param>
        /// <param name="start"></param>
        internal void DrawOtherMaster(Graphics g, int who, int start)
        {
            //非庄家的方位字固定为绿色（忽略 start，庄家由 DrawMaster 单独画红色）
            DrawMasterCell(g, 1, 2, MASTER_ME_X);
            DrawMasterCell(g, 2, 3, MASTER_OTHER_X);
            DrawMasterCell(g, 3, 0, MASTER_WEST_X);
            DrawMasterCell(g, 4, 1, MASTER_EAST_X);
        }


        /// <summary>
        /// 绘制Rank
        /// </summary>
        /// <param name="g">缓冲区图像的Graphics</param>
        /// <param name="me">画我还是画对方</param>
        /// <param name="b">两色还是暗色</param>
        internal void DrawRank(Graphics g, int number, bool me, bool b)
        {
            int X = 0;
            int X2 = 0;
            if (me)
            {
                X = SIDEBAR_ME_X;
                X2 = SIDEBAR_OTHER_X;
            }
            else
            {
                X = SIDEBAR_OTHER_X;
                X2 = SIDEBAR_ME_X;
            }

            Rectangle destRect = new Rectangle(X, SIDEBAR_RANK_Y, SIDEBAR_ICON_SIZE, SIDEBAR_ICON_SIZE);
            Rectangle destRect2 = new Rectangle(X2, SIDEBAR_RANK_Y, SIDEBAR_ICON_SIZE, SIDEBAR_ICON_SIZE);

            if (!b)
            {
                if (me)
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OurCurrentRank, b), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OpposedCurrentRank, b), GraphicsUnit.Pixel);
                }
            }
            else
            {
                if (me)
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OurCurrentRank, b), GraphicsUnit.Pixel);
                    g.DrawImage(Properties.Resources.CardNumber, destRect2, getCardNumberImage(mainForm.currentState.OpposedCurrentRank, !b), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(Properties.Resources.CardNumber, destRect, getCardNumberImage(mainForm.currentState.OpposedCurrentRank, b), GraphicsUnit.Pixel);
                    g.DrawImage(Properties.Resources.CardNumber, destRect2, getCardNumberImage(mainForm.currentState.OurCurrentRank, !b), GraphicsUnit.Pixel);
                }
            }
        }

        private Rectangle getCardNumberImage(int number, bool b)
        {
            Rectangle result = new Rectangle(0, 0, 0, 0);

            if (number >= 0 && number <= 12)
            {
                if (b)
                {
                    number += 14;
                }
                result = new Rectangle(number * SIDEBAR_CELL, 0, SIDEBAR_CELL, SIDEBAR_CELL);
            }


            if ((number == 53) && (b))
            {
                result = new Rectangle(27 * SIDEBAR_CELL, 0, SIDEBAR_CELL, SIDEBAR_CELL);
            }
            if ((number == 53) && (!b))
            {
                result = new Rectangle(13 * SIDEBAR_CELL, 0, SIDEBAR_CELL, SIDEBAR_CELL);
            }

            return result;
        }


        private void ClearSidebarIcon(Graphics g, Rectangle destRect)
        {
            int sidebarX = destRect.X >= SIDEBAR_RIGHT_X ? SIDEBAR_RIGHT_X : SIDEBAR_LEFT_X;
            double scaleX = Properties.Resources.Sidebar.Width / (double)SIDEBAR_W;
            double scaleY = Properties.Resources.Sidebar.Height / (double)SIDEBAR_H;
            Rectangle sourceRect = new Rectangle(
                (int)Math.Round((destRect.X - sidebarX) * scaleX),
                (int)Math.Round((destRect.Y - SIDEBAR_Y) * scaleY),
                (int)Math.Round(destRect.Width * scaleX),
                (int)Math.Round(destRect.Height * scaleY));
            g.DrawImage(Properties.Resources.Sidebar, destRect, sourceRect, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 画花色
        /// </summary>
        /// <param name="g"></param>
        /// <param name="suit">花色</param>
        /// <param name="me">画我方还是对方</param>
        /// <param name="b">是否画亮色</param>
        internal void DrawSuit(Graphics g, int suit, bool me, bool b)
        {
            int X = 0;
            int X2 = 0;
            if (me)
            {
                X = SIDEBAR_ME_X;
                X2 = SIDEBAR_OTHER_X;
            }
            else
            {
                X = SIDEBAR_OTHER_X;
                X2 = SIDEBAR_ME_X;
            }

            Rectangle destRect = new Rectangle(X, SIDEBAR_SUIT_Y, SIDEBAR_ICON_SIZE, SIDEBAR_ICON_SIZE);
            Rectangle redrawRect = new Rectangle(X2, SIDEBAR_SUIT_Y, SIDEBAR_ICON_SIZE, SIDEBAR_ICON_SIZE);
            Rectangle darkSrc = new Rectangle(SUIT_DARK_INDEX * SIDEBAR_CELL, 0, SIDEBAR_CELL, SIDEBAR_CELL);

            ClearSidebarIcon(g, destRect);
            if (!b)
            {
                g.DrawImage(Properties.Resources.Suit, destRect, darkSrc, GraphicsUnit.Pixel);
                return;
            }

            ClearSidebarIcon(g, redrawRect);
            int suitIndex = Math.Max(0, Math.Min(4, suit - 1));
            g.DrawImage(Properties.Resources.Suit, destRect, new Rectangle(suitIndex * SIDEBAR_CELL, 0, SIDEBAR_CELL, SIDEBAR_CELL), GraphicsUnit.Pixel);
            g.DrawImage(Properties.Resources.Suit, redrawRect, darkSrc, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 画工具栏
        /// </summary>
        internal void DrawToolbar()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            g.DrawImage(Properties.Resources.Toolbar, new Rectangle(415, 325, 129, 29), new Rectangle(0, 0, 129, 29), GraphicsUnit.Pixel);
            //画五种暗花色
            DrawToolbarSuits(g, new bool[5]);
            g.Dispose();
        }

        /// <summary>
        /// 擦去工具栏
        /// </summary>
        internal void RemoveToolbar()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            mainForm.DrawBackgroundRegion(g, new Rectangle(415, 325, 129, 29));
            g.Dispose();
        }


        #endregion // 绘制Sidebar和toolbar


        #region 判断是否亮主
        //是否应该亮主,调用算法
        private void DoRankOrNot(CurrentPoker currentPoker, int user)
        {
            //如果打无主，无需再判断
            if (currentPoker.Rank == 53)
                return;


            //如果还未设主，则设主
            if (mainForm.currentState.Suit == 0)
            {
                int suit = Algorithm.ShouldSetRank(mainForm, user);

                if (suit > 0)
                {
                    mainForm.showSuits = 1;
                    mainForm.whoShowRank = user;

                    mainForm.currentState.Suit = suit;

                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = user; //
                    }

                    //既然已经确定了谁亮的，谁是主，打几，那么就画吧

            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

                    //亮主的时候同时画花色,亮色显示在庄家下面
                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, suit, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, suit, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }


                    //画谁亮的主,绿色显示
                    //DrawMaster(g, user, 1);
                    //画庄家，画红色
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    g.Dispose();


                }
            }
            else //是否可以反
            {
                int suit = Algorithm.ShouldSetRankAgain(mainForm, currentPoker);

                

                if (suit > 0)
                {

                    //是否可以加固
                    if ((suit == mainForm.currentState.Suit) && (mainForm.whoShowRank == user) && (!mainForm.gameConfig.CanMyStrengthen))  //如果不允许加固
                    {
                        return;
                    }

                    //非加固时,考虑自反
                    if ((suit != mainForm.currentState.Suit) && (mainForm.whoShowRank == user) && (!mainForm.gameConfig.CanMyRankAgain))  //如果不允许自反
                    {
                        return;
                    }


                    int oldWhoShowRank = mainForm.whoShowRank;
                    int oldMaster = mainForm.currentState.Master;

                    mainForm.showSuits = 2;
                    mainForm.whoShowRank = user;


                    mainForm.currentState.Suit = suit;

                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = user;
                    }



            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

                    //亮主的时候同时画花色,亮色显示在庄家下面
                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, suit, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, suit, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }


                    //清理原来亮的牌
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    //画谁亮的主,绿色显示
                    //DrawMaster(g, user, 1);
                    //画庄家，画红色
                    DrawMaster(g, mainForm.currentState.Master, 1);

                    g.Dispose();



                }
            }

        }

        //判断我是否亮主
        private void MyRankOrNot(CurrentPoker currentPoker)
        {
            //如果打无主，无需再判断
            if (currentPoker.Rank == 53)
                return;
            bool[] suits = Algorithm.CanSetRank(mainForm, currentPoker);

            ReDrawToolbar(suits);


        }

        //画我的工具栏
        internal void ReDrawToolbar(bool[] suits)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            g.DrawImage(Properties.Resources.Toolbar, new Rectangle(415, 325, 129, 29), new Rectangle(0, 0, 129, 29), GraphicsUnit.Pixel);
            //画五种暗花色
            DrawToolbarSuits(g, suits);
            g.Dispose();
        }


        /// <summary>
        /// 判断最后是否有人亮主.
        /// 根据算法，如果无人亮主，则本局流局，重新发牌
        /// </summary>
        /// <returns></returns>
        internal bool DoRankNot()
        {

            if (mainForm.currentState.Suit == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断我是否做了亮主动作.
        /// 在发牌时检测鼠标事件，如果我进行了点击：
        /// 如果我在亮主栏上进行了点击，
        /// 如果我可以亮主，则进行亮主
        /// </summary>
        /// <param name="e"></param>
        internal void IsClickedRanked(MouseEventArgs e)
        {
            bool[] suits = Algorithm.CanSetRank(mainForm, mainForm.currentPokers[0]);

            if (suits[0]) //如果红桃
            {
                Region region = new Region(new Rectangle(417, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;

                    mainForm.currentState.Suit = 1;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                    }
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 1, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 1, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }

                    
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[1]) //如果黑桃
            {
                Region region = new Region(new Rectangle(443, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                    mainForm.currentState.Suit = 2;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                    
                    }


                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 2, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 2, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);


                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[2]) //如果方块
            {
                Region region = new Region(new Rectangle(468, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                    mainForm.currentState.Suit = 3;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                        
                    }


                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 3, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 3, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);


                    
                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[3]) //如果梅花
            {
                Region region = new Region(new Rectangle(493, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits++;
                    mainForm.whoShowRank = 1;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                    mainForm.currentState.Suit = 4;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                        
                    }


                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 4, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 4, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }
                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);

                    
                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
            if (suits[4]) //如果王
            {
                Region region = new Region(new Rectangle(518, 327, 25, 25));
                if (region.IsVisible(e.X, e.Y))
                {
                    mainForm.showSuits = 3;
                    mainForm.whoShowRank = 1;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                    mainForm.currentState.Suit = 5;
                    if ((mainForm.currentRank == 0) && mainForm.isNew)
                    {
                        mainForm.currentState.Master = 1;
                        
                    }



                    if ((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2))
                    {
                        DrawSuit(g, 5, true, true);
                        DrawRank(g, mainForm.currentState.OurCurrentRank, true, true);
                    }
                    else
                    {
                        DrawSuit(g, 5, false, true);
                        DrawRank(g, mainForm.currentState.OpposedCurrentRank, false, true);
                    }

                    DrawMaster(g, mainForm.currentState.Master, 1);
                    DrawOtherMaster(g, mainForm.currentState.Master, 1);


                    ClearSuitCards(g);
                    g.Dispose();
                }
            }
        }
        #endregion // 判断是否亮主


        #region 在各种情况下画自己的牌

        /// <summary>
        /// 发牌期间进行绘制我的区域.
        /// 按照花色和主牌进行区分。
        /// </summary>
        /// <param name="g">缓冲区图片的Graphics</param>
        /// <param name="currentPoker">我当前得到的牌</param>
        /// <param name="index">手中牌的数量</param>
        internal void DrawMyCards(Graphics g, CurrentPoker currentPoker, int index)
        {
            int j = 0;

            //清下面的屏幕（覆盖整块画布，避免多张牌溢出右侧留下残片）
            Rectangle rect = new Rectangle(-mainForm.ContentOffsetX, HAND_Y_LIFTED, mainForm.CanvasWidth, 116);

 mainForm.DrawBackgroundRegion(g, rect);
            //确定绘画起始位置（步长按张数自适应）
            CARD_STEP = CalcCardStep(index);
            int start = Math.Max(2, (MainForm.LogicalWidth - ((index - 1) * CARD_STEP + 71)) / 2);

            //红桃
            j = DrawMyHearts(g, currentPoker, j, start);
            //花色之间加空隙
            j++;


            //黑桃
            j = DrawMyPeachs(g, currentPoker, j, start);
            //花色之间加空隙
            j++;


            //方块
            j = DrawMyDiamonds(g, currentPoker, j, start);
            //花色之间加空隙
            j++;


            //梅花
            j = DrawMyClubs(g, currentPoker, j, start);
            //花色之间加空隙
            j++;

            //Rank(暂不分主、副Rank)
            j = DrawHeartsRank(g, currentPoker, j, start);
            j = DrawPeachsRank(g, currentPoker, j, start);
            j = DrawClubsRank(g, currentPoker, j, start);
            j = DrawDiamondsRank(g, currentPoker, j, start);

            //小王
            j = DrawSmallJack(g, currentPoker, j, start);
            //大王
            j = DrawBigJack(g, currentPoker, j, start);


        }

        //画自己排序好的牌,一般在摸完牌后调用,和出一次牌后调用
        /// <summary>
        /// 在程序底部绘制已经排序好的牌.
        /// 两种情况下会使用这个方法：
        /// 1.收完底准备出牌时
        /// 2.出完一次牌,需要重画底部
        /// </summary>
        /// <param name="currentPoker"></param>
        internal void DrawMySortedCards(CurrentPoker currentPoker, int index)
        {

            //将临时变量清空
            //这三个临时变量记录我手中的牌的位置、大小和是否被点出
            mainForm.myCardsLocation = new ArrayList();
            mainForm.myCardsNumber = new ArrayList();
            mainForm.myCardIsReady = new ArrayList();


            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

            //清下面的屏幕（覆盖整块画布，避免多张牌溢出右侧留下残片）
            Rectangle rect = new Rectangle(-mainForm.ContentOffsetX, HAND_Y_LIFTED, mainForm.CanvasWidth, 116);

 mainForm.DrawBackgroundRegion(g, rect);
            //计算初始位置（步长按张数自适应）
            CARD_STEP = CalcCardStep(index);
            int start = Math.Max(2, (MainForm.LogicalWidth - ((index - 1) * CARD_STEP + 71)) / 2);


            //记录每张牌的X值
            int j = 0;
            //临时变量，用来辅助判断是否某花色缺失
            int k = 0;
            if (mainForm.currentState.Suit == 1)//红桃
            {
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts(g, currentPoker, j, start);

                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);
                j = DrawHeartsRank(g, currentPoker, j, start);
            }
            else if (mainForm.currentState.Suit == 2) //黑桃
            {

                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start);


                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);
                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
            }
            else if (mainForm.currentState.Suit == 3)  //方片
            {

                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start);


                j = DrawClubsRank(g, currentPoker, j, start);
                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);//方块
            }
            else if (mainForm.currentState.Suit == 4)
            {

                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start);


                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);//梅花
            }
            else if (mainForm.currentState.Suit == 5)
            {
                j = DrawMyHearts(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);

                j = DrawHeartsRank(g, currentPoker, j, start);
                j = DrawPeachsRank(g, currentPoker, j, start);
                j = DrawDiamondsRank(g, currentPoker, j, start);
                j = DrawClubsRank(g, currentPoker, j, start);
            }

            //小王
            j = DrawSmallJack(g, currentPoker, j, start);

            //大王
            j = DrawBigJack(g, currentPoker, j, start);

            g.Dispose();
        }

        private static void IsSuitLost(ref int j, ref int k)
        {
            if ((j - k) <= 1)
            {
                j--;
            }
            k = j;
        }

        /// <summary>
        /// 重画我手中的牌.
        /// 在鼠标进行了单击或者右击之后进行绘制。
        /// </summary>
        /// <param name="currentPoker">当前我手中的牌</param>
        /// <param name="index">牌的数量</param>
        internal void DrawMyPlayingCards(CurrentPoker currentPoker)
        {
            int index = currentPoker.Count;


            mainForm.cardsOrderNumber = 0;

            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

            //清下面的屏幕（覆盖整块画布，避免多张牌溢出右侧留下残片）
            Rectangle rect = new Rectangle(-mainForm.ContentOffsetX, HAND_Y_LIFTED, mainForm.CanvasWidth, 116);
            DrawScoreImage(mainForm.Scores);
             mainForm.DrawBackgroundRegion(g, rect);

            CARD_STEP = CalcCardStep(index);
            int start = Math.Max(2, (MainForm.LogicalWidth - ((index - 1) * CARD_STEP + 71)) / 2);

            //Rank(分主、副Rank)
            //记录每张牌的X值
            int j = 0;
            //临时变量，用来辅助判断是否某花色缺失
            int k = 0;

            if (mainForm.currentState.Suit == 1)
            {
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts2(g, currentPoker, j, start);

                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);
                j = DrawHeartsRank2(g, currentPoker, j, start);//红桃
            }
            else if (mainForm.currentState.Suit == 2)
            {

                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start);

                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);
                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);//黑桃
            }
            else if (mainForm.currentState.Suit == 3)
            {

                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start);

                j = DrawClubsRank2(g, currentPoker, j, start);
                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);//方块
            }
            else if (mainForm.currentState.Suit == 4)
            {

                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start);

                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);//梅花
            }
            else if (mainForm.currentState.Suit == 5)
            {
                j = DrawMyHearts2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyPeachs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyDiamonds2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);
                j = DrawMyClubs2(g, currentPoker, j, start) + 1;
                IsSuitLost(ref j, ref k);

                j = DrawHeartsRank2(g, currentPoker, j, start);
                j = DrawPeachsRank2(g, currentPoker, j, start);
                j = DrawDiamondsRank2(g, currentPoker, j, start);
                j = DrawClubsRank2(g, currentPoker, j, start);
            }

            //小王
            j = DrawSmallJack2(g, currentPoker, j, start);

            //大王
            j = DrawBigJack2(g, currentPoker, j, start);


            DrawActionButtons(g);

            g.Dispose();
        }

        internal void DrawActionButtons()
        {
            if (!CanShowActionButtons())
            {
                return;
            }

            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            DrawActionButtons(g);
            g.Dispose();
        }

        internal void HideActionButtons()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            mainForm.DrawBackgroundRegion(g, ActionButtonArea);
            g.Dispose();
        }

        private bool CanShowActionButtons()
        {
            if (mainForm.whoseOrder != 1)
            {
                return false;
            }

            return mainForm.currentState.CurrentCardCommands == CardCommands.WaitingForMySending
                || mainForm.currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards;
        }

        private void DrawActionButtons(Graphics g)
        {
            if (!CanShowActionButtons())
            {
                return;
            }

            bool waitingForPlay = mainForm.currentState.CurrentCardCommands == CardCommands.WaitingForMySending;
            bool canPlay;
            if (waitingForPlay)
            {
                canPlay = TractorRules.IsInvalid(mainForm, mainForm.currentSendCards, 1);
            }
            else
            {
                int selected = 0;
                for (int i = 0; i < mainForm.myCardIsReady.Count; i++)
                {
                    if ((bool)mainForm.myCardIsReady[i])
                    {
                        selected++;
                    }
                }
                canPlay = selected == 8;
            }

            DrawTextButton(g, PlayButtonRect, "出牌", canPlay, false);
            DrawTextButton(g, HintButtonRect, "提示", waitingForPlay, waitingForPlay && mainForm.hintActive);
        }

        private void DrawTextButton(Graphics g, Rectangle rect, string text, bool enabled, bool active)
        {
            Color fill = Color.FromArgb(255, 244, 220, 164);
            Color border = Color.FromArgb(255, 112, 72, 20);
            Color textColor = Color.FromArgb(255, 62, 38, 8);
            if (!enabled)
            {
                fill = Color.FromArgb(210, 224, 221, 210);
                border = Color.FromArgb(210, 145, 141, 130);
                textColor = Color.FromArgb(210, 110, 106, 96);
            }
            else if (active)
            {
                fill = Color.FromArgb(255, 255, 193, 64);
                border = Color.FromArgb(255, 139, 83, 12);
                textColor = Color.FromArgb(255, 48, 28, 4);
            }

            using (SolidBrush brush = new SolidBrush(fill))
            {
                g.FillRectangle(brush, rect);
            }

            using (Pen pen = new Pen(border, 2))
            {
                g.DrawRectangle(pen, rect);
            }

            using (Font font = new Font("Microsoft YaHei", 12.0f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush brush = new SolidBrush(textColor))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                g.DrawString(text, font, brush, rect, format);
            }
        }
        private void My8CardsIsReady(Graphics g)
        {
            //如果等我扣牌
            if ((mainForm.currentState.CurrentCardCommands == CardCommands.WaitingForSending8Cards))
            {
                int total = 0;
                for (int i = 0; i < mainForm.myCardIsReady.Count; i++)
                {
                    if ((bool)mainForm.myCardIsReady[i])
                    {
                        total++;
                    }
                }
                Rectangle pigRect = new Rectangle(296, 300, 53, 46);
                if (total == 8)
                {
                    g.DrawImage(Properties.Resources.Ready, pigRect);
                }
                else
                {
                     mainForm.DrawBackgroundRegion(g, pigRect);

                }
            }
        }


        /// <summary>
        /// 在屏幕中央绘制我出的牌
        /// </summary>
        /// <param name="readys">我出的牌的列表</param>
        internal void DrawMySendedCardsAction(ArrayList readys)
        {
            int start = 314 - ((readys.Count - 1) * PLAYED_STEP + 71) / 2;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), start + i * PLAYED_STEP, 244, 71, 96);
            }
            g.Dispose();


        }

        /// <summary>
        /// 画对家的牌
        /// </summary>
        /// <param name="readys"></param>
        private void DrawFrieldUserSendedCardsAction(ArrayList readys)
        {
            int start = 314 - ((readys.Count - 1) * PLAYED_STEP + 71) / 2;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), start + i * PLAYED_STEP, 130, 71, 96);
            }
            RedrawFrieldUserCardsAction(g, mainForm.currentPokers[1]);


            g.Dispose();
        }
        private void RedrawFrieldUserCardsAction(Graphics g, CurrentPoker cp)
        {
            Rectangle rect = new Rectangle(103, 23, 424, 100);
            mainForm.DrawBackgroundRegion(g, rect);
            if (cp.Count <= 0)
            {
                return;
            }
            const int northAreaWidth = 424;
            int northCardStep = cp.Count > 1 ? Math.Min(13, (northAreaWidth - 71) / (cp.Count - 1)) : 13;
            int northTotalWidth = (cp.Count - 1) * northCardStep + 71;
            int start = 103 + (northAreaWidth - northTotalWidth) / 2 + northTotalWidth - 71;
            for (int i = 0; i < cp.Count; i++) //最多画25张牌
            {
                DrawMyImage(g, mainForm.gameConfig.BackImage, start, 25, 71, 96);
                start -= northCardStep;
            }
        }


        /// <summary>
        /// 画上家应该出的牌
        /// </summary>
        /// <param name="readys"></param>
        private void DrawPreviousUserSendedCardsAction(ArrayList readys)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            int start = 250 - ((readys.Count - 1) * PLAYED_STEP + 71);
            if (start < 104) { start = 104; }
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), start + i * PLAYED_STEP, 192, 71, 96);
            }

            RedrawPreviousUserCardsAction(g, mainForm.currentPokers[2]);

            g.Dispose();
        }
        private void RedrawPreviousUserCardsAction(Graphics g, CurrentPoker cp)
        {
            Rectangle rect = new Rectangle(4, 148, 75, 204);
            mainForm.DrawBackgroundRegion(g, rect);

            if (cp.Count <= 0)
            {
                return;
            }
            const int sideAreaHeight = 204;
            int sideCardStep = cp.Count > 1 ? Math.Min(4, (sideAreaHeight - 96) / (cp.Count - 1)) : 4;
            int sideTotalHeight = (cp.Count - 1) * sideCardStep + 96;
            int start = 148 + (sideAreaHeight - sideTotalHeight) / 2;
            for (int i = 0; i < cp.Count; i++)  //最多画25张,因为多了不好画了，即使收底
            {
                DrawMyImage(g, mainForm.gameConfig.BackImage, 6, start, 71, 96);
                start += sideCardStep;
            }
        }


        /// <summary>
        /// 画下家应该出的牌
        /// </summary>
        /// <param name="readys"></param>
        private void DrawNextUserSendedCardsAction(ArrayList readys)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            int start = 378;
            if (start + (readys.Count - 1) * PLAYED_STEP + 71 > 525) { start = 525 - ((readys.Count - 1) * PLAYED_STEP + 71); }
            for (int i = 0; i < readys.Count; i++)
            {
                DrawMyImage(g, getPokerImageByNumber((int)readys[i]), start + i * PLAYED_STEP, 192, 71, 96);
            }

            RedrawNextUserCardsAction(g, mainForm.currentPokers[3]);


            g.Dispose();
        }
        private void RedrawNextUserCardsAction(Graphics g, CurrentPoker cp)
        {
            Rectangle rect = new Rectangle(552, 148, 75, 204);
            mainForm.DrawBackgroundRegion(g, rect);
            //554, 241 - count * 4, 71, 96
            if (cp.Count <= 0)
            {
                return;
            }
            const int sideAreaHeight = 204;
            int sideCardStep = cp.Count > 1 ? Math.Min(4, (sideAreaHeight - 96) / (cp.Count - 1)) : 4;
            int sideTotalHeight = (cp.Count - 1) * sideCardStep + 96;
            int start = 148 + (sideAreaHeight - sideTotalHeight) / 2 + sideTotalHeight - 96;
            for (int i = 0; i < cp.Count; i++)
            {
                DrawMyImage(g, mainForm.gameConfig.BackImage, 554, start, 71, 96);
                start -= sideCardStep;
            }
        }


        #endregion // 在各种情况下画自己的牌


        #region 画自己的牌面(四种花色、四种花色Rank、大小王)
        private int DrawBigJack(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.BigJack, 53, j, start);
            return j;
        }


        private int DrawSmallJack(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.SmallJack, 52, j, start);
            return j;
        }

        private int DrawDiamondsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.DiamondsRankTotal, mainForm.currentRank + 26, j, start);
            return j;
        }

        private int DrawClubsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.ClubsRankTotal, mainForm.currentRank + 39, j, start);
            return j;
        }

        private int DrawPeachsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.PeachsRankTotal, mainForm.currentRank + 13, j, start);
            return j;
        }

        private int DrawHeartsRank(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            j = DrawMyOneOrTwoCards(g, currentPoker.HeartsRankTotal, mainForm.currentRank, j, start);
            return j;
        }

        private int DrawMyClubs(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.ClubsNoRank[i], i + 39, j, start);
            }
            return j;
        }

        private int DrawMyDiamonds(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.DiamondsNoRank[i], i + 26, j, start);
            }
            return j;
        }

        private int DrawMyPeachs(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.PeachsNoRank[i], i + 13, j, start);

            }
            return j;
        }

        private int DrawMyHearts(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                j = DrawMyOneOrTwoCards(g, currentPoker.HeartsNoRank[i], i, j, start);
            }
            return j;
        }

        //辅助方法
        private int DrawMyOneOrTwoCards(Graphics g, int count, int number, int j, int start)
        {
            //如果是我亮的主，我需要将亮的主往上提一下
            bool b = (number == 52) || (number == 53);
            b = b & (mainForm.currentState.Suit == 5);
            if (number == (mainForm.currentState.Suit-1)*13 + mainForm.currentRank)
            {
                b = true;
            }

            b = b && (mainForm.currentState.CurrentCardCommands == CardCommands.ReadyCards);

            if (count == 1)
            {
                SetCardsInformation(start + j * CARD_STEP, number, false);
                if (mainForm.whoShowRank == 1 && b)
                {
                    if (number == 52 || number == 53)
                    {
                        g.DrawImage(getPokerImageByNumber(number), start + j * CARD_STEP, HAND_Y, 71, 96); //单个的王不被提上
                    }
                    else
                    {
                        g.DrawImage(getPokerImageByNumber(number), start + j * CARD_STEP, HAND_Y_LIFTED, 71, 96);
                    }
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * CARD_STEP, HAND_Y, 71, 96);
                }

                j++;
            }
            else if (count == 2)
            {
                SetCardsInformation(start + j * CARD_STEP, number, false);

                if (mainForm.whoShowRank == 1 && b && mainForm.showSuits >= 1)
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * CARD_STEP, HAND_Y_LIFTED, 71, 96);
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * CARD_STEP, HAND_Y, 71, 96);
                }
                
                j++;
                SetCardsInformation(start + j * CARD_STEP, number, false);
                if (mainForm.whoShowRank == 1 && b && mainForm.showSuits >= 2)
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * CARD_STEP, HAND_Y_LIFTED, 71, 96);
                }
                else
                {
                    g.DrawImage(getPokerImageByNumber(number), start + j * CARD_STEP, HAND_Y, 71, 96);
                }

                j++;
            }
            return j;
        }


        #endregion // 画自己的牌面

        #region 画自己牌面的方法
        private int DrawBigJack2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.BigJack == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, 53, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            else if (currentPoker.BigJack == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, 53, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, 53, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            return j;
        }

        private int DrawSmallJack2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.SmallJack == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, 52, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            else if (currentPoker.SmallJack == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, 52, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, 52, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            return j;
        }

        private int DrawDiamondsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.DiamondsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 26, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            else if (currentPoker.DiamondsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 26, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 26, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            return j;
        }

        private int DrawClubsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.ClubsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 39, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            else if (currentPoker.ClubsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 39, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 39, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            return j;
        }

        private int DrawPeachsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.PeachsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 13, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            else if (currentPoker.PeachsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 13, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank + 13, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            return j;
        }

        private int DrawHeartsRank2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            if (currentPoker.HeartsRankTotal == 1)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            else if (currentPoker.HeartsRankTotal == 2)
            {
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                j = DrawMyOneOrTwoCards2(g, j, mainForm.currentRank, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
            }
            return j;
        }

        private int DrawMyClubs2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.ClubsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 39, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
                else if (currentPoker.ClubsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 39, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i + 39, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
            }
            return j;
        }

        private int DrawMyDiamonds2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.DiamondsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 26, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
                else if (currentPoker.DiamondsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 26, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i + 26, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
            }
            return j;
        }

        private int DrawMyPeachs2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.PeachsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 13, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
                else if (currentPoker.PeachsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i + 13, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i + 13, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
            }
            return j;
        }

        private int DrawMyHearts2(Graphics g, CurrentPoker currentPoker, int j, int start)
        {
            for (int i = 0; i < 13; i++)
            {
                if (currentPoker.HeartsNoRank[i] == 1)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
                else if (currentPoker.HeartsNoRank[i] == 2)
                {
                    j = DrawMyOneOrTwoCards2(g, j, i, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                    j = DrawMyOneOrTwoCards2(g, j, i, start + j * CARD_STEP, HAND_Y_LIFTED + 1, 71, 96) + 1;
                }
            }
            return j;
        }

        //辅助方法
        private int DrawMyOneOrTwoCards2(Graphics g, int j, int number, int x, int y, int width, int height)
        {
            if ((bool)mainForm.myCardIsReady[mainForm.cardsOrderNumber])
            {
                g.DrawImage(getPokerImageByNumber(number), x, y, width, height);
            }
            else
            {
                g.DrawImage(getPokerImageByNumber(number), x, y + 20, width, height);
            }

            mainForm.cardsOrderNumber++;
            return j;
        }
        #endregion // 类似的画自己牌面的方法

        #region 绘制各家出的牌，并计算结果或者通知下一家
        /// <summary>
        /// 画自己出的牌
        /// </summary>
        internal void DrawMyFinishSendedCards()
        {
            HideActionButtons();
            mainForm.hintActive = false;
            //在中央画出点出的牌
            DrawMySendedCardsAction(mainForm.currentSendCards[0]);

            for (int i = 0; i < mainForm.currentSendCards[0].Count; i++)
            {
                mainForm.currentAllSendPokers[0].AddCard((int)mainForm.currentSendCards[0][i]);
            }


            //重画自己手中的牌
            if (mainForm.currentPokers[0].Count > 0)
            {
                DrawMySortedCards(mainForm.currentPokers[0], mainForm.currentPokers[0].Count);
            }
            else //重新下部空间
            {
                Rectangle rect = new Rectangle(-mainForm.ContentOffsetX, HAND_Y_LIFTED, mainForm.CanvasWidth, 116);
                Graphics g = Graphics.FromImage(mainForm.bmp);
                mainForm.MarkRenderScaled(g);
                mainForm.DrawBackgroundRegion(g, rect);
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();

            //计算目前谁的牌最大

            if (mainForm.currentSendCards[3].Count > 0) //是否完成
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();

            }
            else
            {
                mainForm.whoseOrder = 4;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
            }



        }

        /// <summary>
        /// 下家出牌
        /// </summary>
        internal void DrawNextUserSendedCards()
        {
            mainForm.currentState.CurrentCardCommands = CardCommands.Undefined;
            //画NextUser出的牌
            if (mainForm.currentSendCards[0].Count > 0) //随牌
            {
                DrawNextUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 4, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank, mainForm.currentSendCards[mainForm.firstSend - 1].Count));
            }
            else
            {
                DrawNextUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 4, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank));
                mainForm.whoseOrder = 2;
            }

            //考虑是否盖住的问题
            //我已经出牌，应该将我重画
            int myCount = mainForm.currentSendCards[0].Count;
            if (myCount > 0)
            {
                int start = 314 - ((myCount - 1) * PLAYED_STEP + 71) / 2;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                Rectangle rect = new Rectangle(start - 2, 244, myCount * PLAYED_STEP + 71, 106);
                mainForm.DrawBackgroundRegion(g, rect);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[0][i]), start + i * PLAYED_STEP, 244, 71, 96);
                }
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();

            //
            if (mainForm.currentSendCards[1].Count > 0)
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();
            }
            else
            {
                mainForm.whoseOrder = 2;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
            }


        }

        /// <summary>
        /// 对家出牌
        /// </summary>
        internal void DrawFrieldUserSendedCards()
        {
            mainForm.currentState.CurrentCardCommands = CardCommands.Undefined;
            //画FrieldUser出的牌
            if (mainForm.currentSendCards[3].Count > 0) //随牌
            {
                DrawFrieldUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 2, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank, mainForm.currentSendCards[mainForm.firstSend - 1].Count));
            }
            else
            {
                DrawFrieldUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 2, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank));
            }


            //考虑是否盖住的问题
            //如果下家已经出牌，应该将下家重画,重画下家时，有可能盖住我
            int myCount = mainForm.currentSendCards[3].Count;
            if (myCount > 0)
            {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[3][i]), 378 + i * PLAYED_STEP, 192, 71, 96);
                }
                g.Dispose();
            }
            myCount = mainForm.currentSendCards[0].Count;
            if (myCount > 0)
            {
                int start = 314 - ((myCount - 1) * PLAYED_STEP + 71) / 2;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                Rectangle rect = new Rectangle(start - 2, 244, myCount * PLAYED_STEP + 71, 106);
                mainForm.DrawBackgroundRegion(g, rect);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[0][i]), start + i * PLAYED_STEP, 244, 71, 96);
                }
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();

            //
            if (mainForm.currentSendCards[2].Count > 0)
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();
            }
            else
            {
                mainForm.whoseOrder = 3;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForSend;
            }

            //
        }

        /// <summary>
        /// 上家出牌
        /// </summary>
        internal void DrawPreviousUserSendedCards()
        {
            mainForm.currentState.CurrentCardCommands = CardCommands.Undefined;
            //画PreviousUser出的牌
            if (mainForm.currentSendCards[1].Count > 0) //随牌
            {
                DrawPreviousUserSendedCardsAction(Algorithm.MustSendedCards(mainForm, 3, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank, mainForm.currentSendCards[mainForm.firstSend - 1].Count));
            }
            else
            {
                DrawPreviousUserSendedCardsAction(Algorithm.ShouldSendedCards(mainForm, 3, mainForm.currentPokers, mainForm.currentSendCards, mainForm.currentState.Suit, mainForm.currentRank));
            }


            //考虑是否盖住的问题
            //我已经出牌，应该将我重画
            int myCount = mainForm.currentSendCards[0].Count;
            if (myCount > 0)
            {
                int start = 314 - ((myCount - 1) * PLAYED_STEP + 71) / 2;
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                Rectangle rect = new Rectangle(start - 2, 244, myCount * PLAYED_STEP + 71, 106);
                mainForm.DrawBackgroundRegion(g, rect);
                for (int i = 0; i < myCount; i++)
                {
                    DrawMyImage(g, getPokerImageByNumber((int)mainForm.currentSendCards[0][i]), start + i * PLAYED_STEP, 244, 71, 96);
                }
                g.Dispose();
            }

            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();


            //
            if (mainForm.currentSendCards[0].Count > 0)
            {
                mainForm.currentState.CurrentCardCommands = CardCommands.Pause;
                mainForm.SetPauseSet(mainForm.gameConfig.FinishedOncePauseTime, CardCommands.DrawOnceFinished);

                DrawWhoWinThisTime();
            }
            else
            {
                mainForm.whoseOrder = 1;
                mainForm.currentState.CurrentCardCommands = CardCommands.WaitingForMySending;
            }


        }

        //大家都出完一次牌，则计算得分多少，下次该谁出牌
        internal void DrawFinishedOnceSendedCards()
        {
            if (mainForm.currentPokers[0].Count == 0)
            {
                DrawFinishedSendedCards();
                return;
            }


            #region 测试
            if (mainForm.gameConfig.IsDebug)
            {
                int f1 = mainForm.currentPokers[0].Count;
                int f2 = mainForm.currentPokers[1].Count;
                int f3 = mainForm.currentPokers[2].Count;
                int f4 = mainForm.currentPokers[3].Count;

                if (f1 != f2 || f2 != f3 || f3 != f4)
                {
                    int total = mainForm.currentSendCards[mainForm.firstSend - 1].Count;

                    int[] users = CommonMethods.OtherUsers(mainForm.firstSend);


                    if (mainForm.currentSendCards[users[0] - 1].Count != total)
                    {
                        for (int i = 0; i < mainForm.currentSendCards[users[0] - 1].Count; i++)
                        {
                            mainForm.pokerList[users[0] - 1].Add(mainForm.currentSendCards[users[0] - 1][i]);
                            mainForm.currentPokers[users[0] - 1].AddCard((int)mainForm.currentSendCards[users[0] - 1][i]);
                        }
                        mainForm.currentSendCards[users[0] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs2(mainForm, mainForm.currentPokers, users[0], mainForm.currentSendCards[users[0] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }

                    if (mainForm.currentSendCards[users[1] - 1].Count != total)
                    {
                        for (int i = 0; i < mainForm.currentSendCards[users[1] - 1].Count; i++)
                        {
                            mainForm.pokerList[users[1] - 1].Add(mainForm.currentSendCards[users[1] - 1][i]);
                            mainForm.currentPokers[users[1] - 1].AddCard((int)mainForm.currentSendCards[users[1] - 1][i]);
                        }
                        mainForm.currentSendCards[users[1] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs3(mainForm, mainForm.currentPokers, users[1], mainForm.currentSendCards[users[1] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }


                    if (mainForm.currentSendCards[users[2] - 1].Count != total)
                    {
                        for (int i = 0; i < mainForm.currentSendCards[users[2] - 1].Count; i++)
                        {
                            mainForm.pokerList[users[2] - 1].Add(mainForm.currentSendCards[users[2] - 1][i]);
                            mainForm.currentPokers[users[2] - 1].AddCard((int)mainForm.currentSendCards[users[2] - 1][i]);
                        }
                        mainForm.currentSendCards[users[2] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs4(mainForm, mainForm.currentPokers, users[2], mainForm.currentSendCards[users[2] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }

                   
                }
            } 
            #endregion // 测试
 


            //计算该谁发牌
            mainForm.whoseOrder = TractorRules.GetNextOrder(mainForm);

            int newFirst = mainForm.whoseOrder;


            #region 测试
            //if (mainForm.gameConfig.IsDebug) 
            if (1==0)
            {
                if (mainForm.whoIsBigger != newFirst && mainForm.currentSendCards[0].Count == 1)
                {
                    Console.WriteLine("*******************************************************");
                    Console.WriteLine("首先出牌:" + mainForm.firstSend + ", 花色=" + mainForm.currentState.Suit + ", Rank=" + mainForm.currentRank);
                    Console.WriteLine("按步计算最大:" + mainForm.whoIsBigger + ", 最终计算" + newFirst);

                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("自己");
                    for (int i = 0; i < mainForm.pokerList[0].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[0][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[0].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[0][i] + " ");
                    }

                    Console.WriteLine("");
                    Console.WriteLine("对家");
                    for (int i = 0; i < mainForm.pokerList[1].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[1][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[1].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[1][i] + " ");
                    }

                    Console.WriteLine("");
                    Console.WriteLine("西家");
                    for (int i = 0; i < mainForm.pokerList[2].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[2][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[2].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[2][i] + " ");
                    }

                    Console.WriteLine("");
                    Console.WriteLine("东家");
                    for (int i = 0; i < mainForm.pokerList[3].Count; i++)
                    {
                        Console.Write(mainForm.pokerList[3][i] + " ");
                    }
                    Console.WriteLine("");
                    for (int i = 0; i < mainForm.currentSendCards[3].Count; i++)
                    {
                        Console.Write(mainForm.currentSendCards[3][i] + " ");
                    }
                    Console.WriteLine("");
                    Console.WriteLine("*******************************************************");

                    //复原
                    int[] users = CommonMethods.OtherUsers(mainForm.firstSend);

                    int tmp = mainForm.whoIsBigger;
                    if (mainForm.firstSend == tmp)
                    {
                        tmp = newFirst;
                    }

                    if (tmp == users[0])
                    {
                        mainForm.pokerList[users[0] - 1].Add(mainForm.currentSendCards[users[0] - 1][0]);
                        mainForm.currentPokers[users[0] - 1].AddCard((int)mainForm.currentSendCards[users[0] - 1][0]);
                        mainForm.currentSendCards[users[0] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs2(mainForm, mainForm.currentPokers, users[0], mainForm.currentSendCards[users[0] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }
                    if (tmp == users[1])
                    {
                        mainForm.pokerList[users[1] - 1].Add(mainForm.currentSendCards[users[1] - 1][0]);
                        mainForm.currentPokers[users[1] - 1].AddCard((int)mainForm.currentSendCards[users[1] - 1][0]);
                        mainForm.currentSendCards[users[1] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs3(mainForm, mainForm.currentPokers, users[1], mainForm.currentSendCards[users[1] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }
                    if (tmp == users[2])
                    {
                        mainForm.pokerList[users[2] - 1].Add(mainForm.currentSendCards[users[2] - 1][0]);
                        mainForm.currentPokers[users[2] - 1].AddCard((int)mainForm.currentSendCards[users[2] - 1][0]);
                        mainForm.currentSendCards[users[2] - 1] = new ArrayList();
                        MustSendCardsAlgorithm.WhoseOrderIs4(mainForm, mainForm.currentPokers, users[2], mainForm.currentSendCards[users[2] - 1], 1, mainForm.currentState.Suit, mainForm.currentRank, CommonMethods.GetSuit((int)mainForm.currentSendCards[mainForm.firstSend - 1][0]));
                    }
                    mainForm.timer.Stop();
                }
            }
            #endregion // 测试




            mainForm.whoIsBigger = 0;


            mainForm.firstSend = mainForm.whoseOrder;
            bool success = false;
            if (((mainForm.currentState.Master == 1) || (mainForm.currentState.Master == 2)) && ((newFirst == 1) || (newFirst == 2)))
            {
                success = true;
            }
            if (((mainForm.currentState.Master == 3) || (mainForm.currentState.Master == 4)) && ((newFirst == 3) || (newFirst == 4)))
            {
                success = true;
            }


            if (!success)
            {
                TractorRules.CalculateScore(mainForm);

            }

            mainForm.currentSendCards[0] = new ArrayList(); 
            mainForm.currentSendCards[1] = new ArrayList(); 
            mainForm.currentSendCards[2] = new ArrayList(); 
            mainForm.currentSendCards[3] = new ArrayList(); 

            DrawCenterImage();
            DrawScoreImage(mainForm.Scores);
            mainForm.Refresh();



        }

        private void DrawWhoWinThisTime()
        {
            //谁赢了这一圈
            int whoWin = TractorRules.GetNextOrder(mainForm);

            if (whoWin == 1) //我
            {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                g.DrawImage(Properties.Resources.Winner, WINNER_ME_X, WINNER_ME_Y, WINNER_W, WINNER_H);
                g.Dispose();
            }
            else if (whoWin == 2) //对家
            {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                g.DrawImage(Properties.Resources.Winner, WINNER_OTHER_X, WINNER_OTHER_Y, WINNER_W, WINNER_H);
                g.Dispose();
            }
            else if (whoWin == 3) //西家
            {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                g.DrawImage(Properties.Resources.Winner, WINNER_WEST_X, WINNER_WEST_Y, WINNER_W, WINNER_H);
                g.Dispose();
            }
            else if (whoWin == 4) //东家
            {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
                g.DrawImage(Properties.Resources.Winner, WINNER_EAST_X, WINNER_EAST_Y, WINNER_W, WINNER_H);
                g.Dispose();
            }

            mainForm.Refresh();
        }

        internal void DrawScoreImage(int scores)
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);
            Bitmap bmp = global::Kuaff.Tractor.Properties.Resources.scores;
            Font font = new Font("宋体", 12, FontStyle.Bold);

            if (mainForm.currentState.Master == 2 || mainForm.currentState.Master == 4)
            {
                Rectangle rect = new Rectangle(490, 128, 56, 56);
                g.DrawImage(bmp, rect);
                int x = 506;
                if (scores.ToString().Length ==2)
                {
                    x -= 4;
                }
                else if (scores.ToString().Length ==3)
                {
                    x -= 8;
                }
                g.DrawString(scores + "", font, Brushes.White, x, 138);
            }
            else
            {
                Rectangle rect = new Rectangle(85, 300, 56, 56);
                g.DrawImage(bmp, rect);
                int x = 100;
                if (scores.ToString().Length == 2)
                {
                    x -= 4;
                }
                else if (scores.ToString().Length == 3)
                {
                    x -= 8;
                }
                g.DrawString(scores + "", font, Brushes.White, x, 310);
            }

            g.Dispose();
        }

        internal void DrawFinishedScoreImage()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

            Pen pen = new Pen(Color.White, 2);
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, Color.White)), 77, 124, 476, 244);
            g.DrawRectangle(pen, 77, 124, 476, 244);

            //画底牌,从169开始画
            for (int i = 0; i < 8; i++)
            {
                g.DrawImage(getPokerImageByNumber((int)mainForm.send8Cards[i]), 230 + i * 14, 130, 71, 96);
            }

            //画小丫
            g.DrawImage(global::Kuaff.Tractor.Properties.Resources.Logo, 160, 237, 110, 112);

            //画得分
            Font font = new Font("宋体", 16, FontStyle.Bold);
            g.DrawString("总得分 " + mainForm.Scores, font, Brushes.Blue, 310, 286);

            g.Dispose();
        }

        //大家都出完牌，则计算得分多少，下次该谁出牌
        internal void DrawFinishedSendedCards()
        {
            mainForm.isNew = false;

           //计算得分，确定下一次庄家，确定下一次的Rank
            TractorRules.GetNextMasterUser(mainForm);


            mainForm.currentSendCards[0] = new ArrayList(); 
            mainForm.currentSendCards[1] = new ArrayList(); 
            mainForm.currentSendCards[2] = new ArrayList(); 
            mainForm.currentSendCards[3] = new ArrayList(); 

            DrawCenterImage();
            DrawFinishedScoreImage();
            mainForm.Refresh();

            mainForm.SetPauseSet(mainForm.gameConfig.FinishedThisTime, CardCommands.DrawOnceRank);

           
        }
        #endregion // 绘制各家出的牌，并计算结果或者通知下一家


        #region 画牌时的辅助方法

        //根据牌号得到相应的牌的图片
        private Bitmap getPokerImageByNumber(int number)
        {
            Bitmap bitmap = null;

            if (mainForm.gameConfig.CardImageName.Length == 0) //从内嵌的图案中读取
            {
                 bitmap = (Bitmap)mainForm.gameConfig.CardsResourceManager.GetObject("_" + number, Kuaff_Cards.Culture);
            }
            else
            {
                bitmap = mainForm.cardsImages[number]; //从自定义的图片中读取
            }

            return bitmap;
        }

        /// <summary>
        /// 重画程序背景
        /// </summary>
        /// <param name="g">缓冲区图像的Graphics</param>
        internal void DrawBackground(Graphics g)
        {
            //Bitmap image = global::Kuaff.Tractor.Properties.Resources.Backgroud;
            mainForm.FillBackground(g);
        }

        //发牌动画已精简化，避免在超采样缓冲区与侧栏区域产生残影与缺口
        private void DrawAnimatedCard(Bitmap card, int x, int y, int width, int height)
        {
        }

        //画图的方法
        private void DrawMyImage(Graphics g, Bitmap bmp, int x, int y, int width, int height)
        {
            g.DrawImage(bmp, x, y, width, height);
        }

        //设置当前的牌的信息
        private void SetCardsInformation(int x, int number, bool ready)
        {
            mainForm.myCardsLocation.Add(x);
            mainForm.myCardsNumber.Add(number);
            mainForm.myCardIsReady.Add(ready);
        }
        #endregion // 画牌时的辅助方法



        //测试方法
        internal void TestCards()
        {
            Graphics g = Graphics.FromImage(mainForm.bmp);
            mainForm.MarkRenderScaled(g);

            int count = mainForm.pokerList[0].Count;
            Font font = new Font("宋体", 9);

            g.DrawString("自己：", font, Brushes.Red, 80, 130);
            g.DrawString("对家：", font, Brushes.Red, 80, 170);
            g.DrawString("西家：", font, Brushes.Red, 80, 210);
            g.DrawString("东家：", font, Brushes.Red, 80, 250);


            Console.Write("自己：");
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[0][i].ToString(), font, Brushes.Red, 120 + i * 15, 130);
                Console.Write(mainForm.pokerList[0][i].ToString() + ",");
            }
            Console.Write("\r\n对家：");
            count = mainForm.pokerList[1].Count;
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[1][i].ToString(), font, Brushes.Red, 120 + i * 15, 170);
                Console.Write(mainForm.pokerList[1][i].ToString() + ",");
            }
            Console.Write("\r\n西家：");
            count = mainForm.pokerList[2].Count;
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[2][i].ToString(), font, Brushes.Red, 120 + i * 15, 210);
                Console.Write(mainForm.pokerList[2][i].ToString() + ",");
            }
            Console.Write("\r\n东家：");
            count = mainForm.pokerList[3].Count;
            for (int i = 0; i < count; i++)
            {
                g.DrawString(mainForm.pokerList[3][i].ToString(), font, Brushes.Red, 120 + i * 15, 250);
                Console.Write(mainForm.pokerList[3][i].ToString() + ",");
            }

            mainForm.Refresh();
        }
        private void DrawToolbarSuits(Graphics g, bool[] suits)
        {
            for (int i = 0; i < 5; i++)
            {
                Rectangle destRect = new Rectangle(417 + i * 25, 327, 25, 25);
                Rectangle srcRect = new Rectangle(i * SIDEBAR_CELL, 0, SIDEBAR_CELL, SIDEBAR_CELL);
                if (suits[i])
                {
                    g.DrawImage(Properties.Resources.Suit, destRect, srcRect, GraphicsUnit.Pixel);
                }
                else
                {
                    using (ImageAttributes attributes = new ImageAttributes())
                    {
                        ColorMatrix matrix = new ColorMatrix();
                        matrix.Matrix33 = 0.45f;
                        attributes.SetColorMatrix(matrix);
                        g.DrawImage(Properties.Resources.Suit, destRect, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel, attributes);
                    }
                }
            }
        }
    }
}
