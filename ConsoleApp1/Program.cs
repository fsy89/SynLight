using System.Collections.Generic;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System;
using System.Net.Sockets;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace ConsoleApp1
{
    class Program
    {
        #region Variables specific to the setup
        const int Width = 18; //number of LEDs in X
        const int Height = 12; //number of LEDs in Y
        const int Shift = 0; //change to match 21/9 content on 16/9 screen
        const int UpDown = 0; //change if first LED is not in the corner
        const bool Clockwise = false; //Clockwise of counter-clockwise
        const int Corner = 0; //Number of LEDs that should be needed to complete the strip to the very corner
        const int StartCorner = (int)corner.bottomRight; //Position of the first LED
        static readonly EndPoint ESP = new IPEndPoint(new IPAddress(new byte[4] { 192, 168, 0, 30 }), 8787); //ESP IP and port 8787
        const bool LowPassFilter = true; //True = apply low-pass filter, false = does not apply low-pass filter
        const bool FluxFilter = true;
        const bool useDynamicTiming = true; //False : will use fixed timing defined by SleepingTime
        const int minIdleValue = 2;
        const int maxIdleValue = 150;
        const int SleepingTime = 5; //milli-seconds idle time, reduces CPU usage and frequency
        #endregion

        #region Variables not to touch
        static Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp);
        /*static Rectangle edgeLeft;
        static Rectangle edgeRight;
        static Rectangle edgeTop;
        static Rectangle edgeBot;
        static Bitmap bmpScreenshot;*/
        static Bitmap scaledBmpScreenshot;
        //static Bitmap secondScaledBmpScreenshot;
        static Bitmap scalededgeLeft;
        static Bitmap scalededgeRight;
        static Bitmap scalededgeTop;
        static Bitmap scalededgeBot;
        /*static bool screenConfigured = false;
        static bool debug = false;
        static int startX;
        static int startY;
        static int endX;
        static int endY;
        static int hX;
        static int hY;
        static Rectangle rect;
        static Bitmap bmp;*/

        /*static double sRed = 255;
        static double sGreen = 255;
        static double sBlue = 255;*/
        static List<byte> LastByteToSend = new List<byte>(0);
        static List<byte> newByteToSend = new List<byte>(0);
        static List<byte> byteToSend;

        static DateTime startTime = DateTime.Parse("00:00:00");
        static DateTime midDay = DateTime.Parse("12:00:00");
        static DateTime endTime = DateTime.Parse("05:00:00");
        const double nbMinutesStart = 200.0;
        const double nmMinutesStop = 250.0;
        static int startTimeInt;
        static int endTimeInt;
        static int currentTimeInt;

        static double fluxRatio = 1.0;

        static int difference = 0;
        static int currentSleepTime = 5;       

        enum corner
        {
            bottomRight = 0, bottomLeft, topLeft, topRight
        }
        enum PayloadType
        {
            ping = 0,
            fixedColor = 1,
            multiplePayload = 2,
            terminalPayload = 3,
        }
        #endregion
        static long tickCount = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("- Starting -");
            Console.WriteLine("Width\t\t= " + Width);
            Console.WriteLine("Height\t\t= " + Height);
            Console.WriteLine("Shift\t\t= " + Shift);
            Console.WriteLine("UpDown\t\t= " + UpDown);
            Console.WriteLine("Clockwise\t= " + Clockwise);
            Console.WriteLine("Corner\t\t= " + Corner);
            Console.WriteLine("ESP\t\t= " + ESP);

            while (true)
            {
                Console.WriteLine("- Tick - " + tickCount++ + "\t currentSleepTime = " + currentSleepTime);
                Tick();
                if(useDynamicTiming)
                {
                    System.Threading.Thread.Sleep(currentSleepTime);
                }
                else
                {
                    System.Threading.Thread.Sleep(SleepingTime); 
                }
            }
        }
        static void Tick()
        {
            GetScreenShot();
            ProcessScreenShot();
            Send();
        }
        static void GetScreenShot()
        {
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
                if (false)
                {
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
        static Bitmap Resize(Bitmap srcImage)
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
        static Bitmap ResizeSizes(Bitmap srcImage)
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
        static Bitmap ResizeTops(Bitmap srcImage)
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
        static void ProcessScreenShot()
        {
            try
            {
                byteToSend = new List<byte>();
                int subCorner = Math.Max(0, Corner - 1);
                if (Clockwise)
                {
                    if (StartCorner == corner.topLeft)
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
                    if (StartCorner == corner.topRight)
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
                    if (StartCorner == corner.bottomRight)
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
                    if (StartCorner == corner.bottomLeft)
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
                    if (StartCorner == corner.topLeft)
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
                    if (StartCorner == corner.topRight)
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
                    if (StartCorner == corner.bottomRight)
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
                    if (StartCorner == corner.bottomLeft)
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

        static void Send()
        {            
            newByteToSend = new List<byte>(0);
            if (LowPassFilter)//low-pass filter enabled by default
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


            if (FluxFilter)
            {
                startTimeInt = startTime.Hour * 60 + startTime.Minute;
                endTimeInt = endTime.Hour * 60 + endTime.Minute;
                currentTimeInt = DateTime.Now.Hour * 60 + DateTime.Now.Minute;

                if (currentTimeInt > startTimeInt && currentTimeInt < endTimeInt)
                {
                    fluxRatio = (nmMinutesStop - nbMinutesStart) / nmMinutesStop;
                }
                else if (Math.Abs(currentTimeInt - endTimeInt) < nbMinutesStart)
                {
                    fluxRatio = (nmMinutesStop - nbMinutesStart + double.Parse(Math.Abs(currentTimeInt - endTimeInt).ToString())) / nmMinutesStop;
                }
                else if (currentTimeInt > midDay.Hour * 60 + midDay.Minute)
                {
                    currentTimeInt = (24 * 60) - currentTimeInt;
                    if (Math.Abs(currentTimeInt - startTimeInt) < nbMinutesStart)
                    {
                        fluxRatio = (nmMinutesStop - nbMinutesStart + double.Parse(Math.Abs(currentTimeInt - startTimeInt).ToString())) / nmMinutesStop;
                    }
                }
                for (int n = 2; n < byteToSend.Count - 2; n += 3)
                {
                    string s;
                    byte b;

                    s = (newByteToSend[n] * fluxRatio).ToString().Split(',')[0];
                    b = byte.Parse(s);
                    newByteToSend[n] = b;

                    s = (newByteToSend[n - 1] * ((1 + fluxRatio) / 2)).ToString().Split(',')[0];
                    b = byte.Parse(s);
                    newByteToSend[n - 1] = b;

                    s = (newByteToSend[n - 2] * (1 + (1 - fluxRatio) / 3)).ToString().Split(',')[0];
                    short i = short.Parse(s);
                    if (i > 255)
                    {
                        i = 255;
                    }
                    b = byte.Parse(i.ToString());
                    newByteToSend[n - 2] = b;
                }
            }

            int packetSize = 1200;
            for (int n = 0; n + packetSize <= byteToSend.Count; n += packetSize)
            {
                SendPayload(PayloadType.multiplePayload, newByteToSend.GetRange(n, packetSize));
            }
            int index = newByteToSend.Count - (newByteToSend.Count % packetSize);
            SendPayload(PayloadType.terminalPayload, newByteToSend.GetRange(index, newByteToSend.Count % packetSize));

            GetIdle();
        }
        
        static void RotateArray()
        {
            List<byte> byteToSend2 = new List<byte>(newByteToSend);

            for (int n = 0; n < newByteToSend.Count; n++)
            {
                byteToSend2[n] = newByteToSend[(n + UpDown * 3) % (byteToSend.Count - 1)];
            }

            newByteToSend = new List<byte>(byteToSend2);
        }
        
        static void SendPayload(PayloadType plt, List<byte> payload)
        {
            payload.Insert(0, (byte)plt);
            payload.Insert(0, (byte)('A')); //magic number #1, helps eliminate the junk that is broadcasted on the network

            sock.SendTo(payload.ToArray(), ESP);
        }

        static void GetIdle()
        {
            if (newByteToSend.Count != byteToSend.Count)
            {
                difference = 150;
            }
            else
            {
                difference = 0;
                for (int n = 0; n < byteToSend.Count; n++)
                {
                    difference += Math.Abs((int)(byteToSend[n]) - (int)(newByteToSend[n]));
                }
            }

            double tmp = difference*5;
            //tmp = Math.Max(0,Math.Min(5000.0,tmp));
            tmp = (-tmp + 1000.0) / 500.0;
            currentSleepTime += (int)tmp;
            currentSleepTime = Math.Min(Math.Max(minIdleValue, currentSleepTime),maxIdleValue);
        }
    }
}