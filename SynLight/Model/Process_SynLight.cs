using System;
using System.Collections.Generic;
using System.Windows;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace SynLight.Model
{
    public class Process_SynLight : Param_SynLight
    {
        

        public Process_SynLight()
        {
            bmpScreenshot = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
            process = new Thread(CheckMethod);
            process.Start();
        }

        #region Privates methodes
        private void CheckMethod()
        {
                while(!Connected)
                {
                //IF NOT CONNECTED, TRY TO RECONNECT
                    Tittle = "SynLight - Trying to connect ...";
                    Thread.Sleep(2000);
                    FindNodeMCU();
                }
                while (PlayPause)
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    if (Index == 0)
                    {
                        Tick();
                    }
                    else if (Index == 1)
                    {
                        SingleColor();
                    }

                    Thread.Sleep(currentSleepTime);
                    GC.Collect(); //COUPLES OF MB WON
                    watch.Stop();

                    int Hz = (int)(1000.0 / watch.ElapsedMilliseconds);
                    Tittle = "Synlight - " + Hz.ToString() + "Hz";
                }
                SendPayload(PayloadType.fixedColor, 0);
                process = new Thread(CheckMethod);
        }
        private void Tick()
        {
            if (!edges)
            {
                GetScreenShot();
            }
            else
            {
                GetScreenShotedges();
            }
            ProcessScreenShot();
            Send();
        }
        private void GetScreenShot()
        {
            try
            {
                Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot); //1
                gfxScreenshot.CopyFromScreen(0, 0, 0, 0, currentScreen);
                scaledBmpScreenshot = new Bitmap(bmpScreenshot, Width, Height);
                gfxScreenshot.Clear(Color.Empty);
                //Resize(scaledBmpScreenshot).Save("6regular.bmp");
            }
            catch
            {
                scaledBmpScreenshot = new Bitmap(1, 1);
                scaledBmpScreenshot.SetPixel(0, 0, Color.Black);
            }
        }        
        private void GetScreenShotedges()
        {
            try
            {
                #region old single screen
                /*
                Rectangle screenToCapture;
                int xScreen = (int)(SystemParameters.PrimaryScreenWidth);
                int yScreen = (int)(SystemParameters.PrimaryScreenHeight);

                Rectangle rect = new Rectangle(0, 0, xScreen / Width, yScreen);
                Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmp);
                gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeLeft = new Bitmap(bmp, 1, Height);
                gfxScreenshot.Clear(Color.Empty);

                rect = new Rectangle(xScreen - (xScreen / Width) - 1, 0, xScreen / Width, yScreen);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot2 = Graphics.FromImage(bmp);
                gfxScreenshot2.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeRight = new Bitmap(bmp, 1, Height);
                gfxScreenshot2.Clear(Color.Empty);

                rect = new Rectangle(0, 0, xScreen, yScreen / Height);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot3 = Graphics.FromImage(bmp);
                gfxScreenshot3.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeTop = new Bitmap(bmp, Width, 1);
                gfxScreenshot3.Clear(Color.Empty);

                rect = new Rectangle(0, yScreen - (yScreen / Height) - 1, xScreen, yScreen / Height);
                bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
                Graphics gfxScreenshot4 = Graphics.FromImage(bmp);
                gfxScreenshot4.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeBot = new Bitmap(bmp, Width, 1);
                gfxScreenshot4.Clear(Color.Empty);
                */
                /*t xScreen = (scannedArea.Width-scannedArea.X);
                int yScreen = (scannedArea.Height-scannedArea.Y);*/
                #endregion

                //MULTIPLE MONITORS FIESTA
                startX = scannedArea.X;
                startY = scannedArea.Y;
                endX = scannedArea.Width;
                endY = scannedArea.Height;
                hX = endX - startX;
                hY = endY - startY;

                startY += ((Shifting * hY) / Height)/2;
                endY -= ((Shifting * hY) / Height) / 2;

                rect = new Rectangle(startX, startY, startX+(hX/Width), endY);
                bmp = new Bitmap(hX/Width, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot = Graphics.FromImage(bmp);
                gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeLeft = new Bitmap(bmp, 1, Height);
                gfxScreenshot.Clear(Color.Empty);

                rect = new Rectangle(endX-(hX/Width), startY, startX + (hX / Width), endY);
                bmp = new Bitmap(hX/Width, hY, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot2 = Graphics.FromImage(bmp);
                gfxScreenshot2.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeRight = new Bitmap(bmp, 1, Height);
                gfxScreenshot2.Clear(Color.Empty);

                rect = new Rectangle(startX, startY, endX, startY+(hY/Height));
                bmp = new Bitmap(hX, hY/Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot3 = Graphics.FromImage(bmp);
                gfxScreenshot3.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeTop = new Bitmap(bmp, Width, 1);
                gfxScreenshot3.Clear(Color.Empty);

                rect = new Rectangle(startX, endY- (hY / Height), endX, endY);
                bmp = new Bitmap(hX, hY/ Height, PixelFormat.Format32bppRgb);
                Graphics gfxScreenshot4 = Graphics.FromImage(bmp);
                gfxScreenshot4.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                scalededgeBot = new Bitmap(bmp, Width, 1);
                gfxScreenshot4.Clear(Color.Empty);

                scaledBmpScreenshot = new Bitmap(Width, Height);

                for (int n = 0; n < scalededgeLeft.Height; n++)
                {
                    scaledBmpScreenshot.SetPixel(0, n, scalededgeLeft.GetPixel(0, n));
                    scaledBmpScreenshot.SetPixel(Width - 1, n, scalededgeRight.GetPixel(0, n));
                }
                for (int n = 1; n < scalededgeTop.Width - 1; n++)
                {
                    scaledBmpScreenshot.SetPixel(n, 0, scalededgeTop.GetPixel(n, 0));
                    scaledBmpScreenshot.SetPixel(n, Height - 1, scalededgeBot.GetPixel(n, 0));
                }
                try
                {
                    if (debug)
                    {
                        debug = false;
                        ResizeSizes(scalededgeLeft).Save("1Left.bmp");
                        ResizeSizes(scalededgeRight).Save("3Right.bmp");
                        ResizeTops(scalededgeTop).Save("2Top.bmp");
                        ResizeTops(scalededgeBot).Save("4Bot.bmp");
                        Resize(scaledBmpScreenshot).Save("5full.bmp");
                    }
                }
                catch
                {
                }
            }
            catch
            {
                scalededgeLeft = new Bitmap(1, 1);
                scalededgeLeft.SetPixel(0, 0, Color.Black);
            }
        }
        private Bitmap Resize(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.NearestNeighbor;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight));
            }
            return newImage;
        }
        private Bitmap ResizeSizes(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth / Width, (int)SystemParameters.PrimaryScreenHeight);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.NearestNeighbor;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth / Width, (int)SystemParameters.PrimaryScreenHeight));
            }
            return newImage;
        }
        private Bitmap ResizeTops(Bitmap srcImage)
        {
            Bitmap newImage = new Bitmap((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight / Height);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.NearestNeighbor;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(srcImage, new Rectangle(0, 0, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight / Height));
            }
            return newImage;
        }
        private void ProcessScreenShot()
        {
            try
            {
                byteToSend = new List<byte>();
                int subCorner = Math.Max(0, Corner - 1);
                if (Clockwise)
                {
                    if (TopLeft)
                    {
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                    }
                    if (TopRight)
                    {
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                    }
                    if (BotRight)
                    {
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                    }
                    if (BotLeft)
                    {
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                    }
                }
                else
                {
                    if (TopLeft)
                    {
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                    }
                    if (TopRight)
                    {
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                    }
                    if (BotRight)
                    {
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                    }
                    if (BotLeft)
                    {
                        for (int x = Corner; x < scaledBmpScreenshot.Width - 1 - Corner; x++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, scaledBmpScreenshot.Height - 1).B));
                        }
                        for (int y = scaledBmpScreenshot.Height - 1 - subCorner; y > subCorner; y--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(scaledBmpScreenshot.Width - 1, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                        for (int x = scaledBmpScreenshot.Width - 1 - Corner; x > Corner; x--)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(x, 0).B));
                        }
                        for (int y = subCorner; y < scaledBmpScreenshot.Height - 1 - subCorner; y++)
                        {
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).R));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).G));
                            byteToSend.Add((scaledBmpScreenshot.GetPixel(0, Math.Max(0, Math.Min(y, scaledBmpScreenshot.Height - 1))).B));
                        }
                    }
                }
            }
            catch
            {
            }
        }
        private void SingleColor()
        {
            byteToSend = new List<byte>() { Red, Green, Blue };
            if (staticColorChanged)
            {
                staticColorChanged = false;
                SendPayload(PayloadType.fixedColor, byteToSend);
                staticColorCurrentTime = 0;
                currentSleepTime = (((currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Properties.Settings.Default.maxTime - difference)) / 4) + (int)(cpuCounter.NextValue() * 2))/2;
            }
            else
            {
                staticColorCurrentTime++;
                if (staticColorCurrentTime > staticColorMaxTime)
                {
                    SendPayload(PayloadType.fixedColor, byteToSend);
                    staticColorCurrentTime = 0;
                }
                currentSleepTime = 100;
            }
        }
        private void Send()
        {
            #region If the screen is black ...
            black = true;
            foreach (byte b in byteToSend)
            {
                if (b != 0)
                {
                    black = false;
                    break;
                }
            }
            if (black)
            {
                if (justBlack++>5)
                {
                    justBlack = 0;
                    sock.SendTo(new byte[] { (byte)PayloadType.fixedColor, 5 }, endPoint);
                }
            } 
            #endregion

            else
            {
                NewToSend();
                newByteToSend = new List<byte>(0);
                if (LPF)
                {
                    for (int n = byteToSend.Count; n < LastByteToSend.Count && n < byteToSend.Count + 6; n++) { LastByteToSend.Add(0); }
                    for (int n = 0; n < byteToSend.Count; n++) { newByteToSend.Add((byte)(byteToSend[n] >> 1)); }
                    for (int n = 0; n < byteToSend.Count && n < LastByteToSend.Count; n++) { newByteToSend[n] += (byte)(LastByteToSend[n] >> 1); }
                    LastByteToSend = new List<byte>(byteToSend);
                }
                else
                {
                    LastByteToSend = newByteToSend = byteToSend;
                }
                if (UpDown != 0)
                {
                    RotateArray();
                }

                //TODO
                for (int n = 0; n+packetSize <= byteToSend.Count; n += packetSize)
                {
                    SendPayload(PayloadType.multiplePayload, newByteToSend.GetRange(n, packetSize));
                }
                int index = newByteToSend.Count - (newByteToSend.Count % packetSize);
                SendPayload(PayloadType.terminalPayload, newByteToSend.GetRange(index, newByteToSend.Count%packetSize));
            }

            //IDLE TIME TO REDUCE CPU USAGE WHEN THE FRAMES AREN'T CHANGING MUCH AND WHEN CPU USAGE IS HIGH
            currentSleepTime = (((currentSleepTime + Math.Max(Properties.Settings.Default.minTime, Properties.Settings.Default.maxTime - difference)) / 4) + (int)(cpuCounter.NextValue() * 2))/3;
        }
        private void RotateArray()
        {
            List<byte> byteToSend2 = new List<byte>(newByteToSend);

            for (int n = 0; n < newByteToSend.Count; n++)
            {
                byteToSend2[n] = newByteToSend[(n + UpDown * 3) % (byteToSend.Count - 1)];
            }

            newByteToSend = new List<byte>(byteToSend2);
        }
        private void NewToSend()
        {
            if (LastByteToSend.Count != byteToSend.Count)
            {
                difference = Properties.Settings.Default.maxTime;
                return;
            }
            else
            {
                difference = 0;
            }
            for (int n = 0; n < byteToSend.Count; n++)
            {
                difference += Math.Abs((int)(byteToSend[n]) - (int)(LastByteToSend[n]));
            }
        }
        #endregion
    }
}
