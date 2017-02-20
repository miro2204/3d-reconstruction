using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace _3D_reconstruction
{
    public class EMGUoperation   //singelton
    {
        private EMGUoperation() { }
        private static volatile EMGUoperation instance;
        private static readonly object syncRoot = new Object();

        public static EMGUoperation Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new EMGUoperation();
                    }
                }
                return instance;
            }
        }

        public Rectangle getRectFromImage(string FileName, Image<Bgr, Byte> Image)
        {
            var cascade = new HaarCascade(FileName);
            var gray = Image.Convert<Gray, Byte>();
            var MouthDetected = cascade.Detect(gray, 1.1, 10, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            gray.ROI = Rectangle.Empty;
            var Rect = new Rectangle(0, 0, Image.Width, Image.Height);
            foreach (var m in MouthDetected)
            {
                Rect = m.rect;
                break;
            }
            return Rect;
        }

        public Image<Gray, byte> CannyImage(Image<Bgr, byte> image, int Value1, int Value2)
        {
            var frame = image;
            var grayFrame = frame.Convert<Gray, Byte>();
            var smallGrayFrame = grayFrame.PyrDown();
            var smoothedGrayFrame = smallGrayFrame.PyrUp();
            var cannyFrame = smoothedGrayFrame.Canny(new Gray(Value1), new Gray(Value2));
            return cannyFrame;
        }

        public Rectangle findnose(Image<Gray, byte> imgnose, int minY, ref Point ptnose, ref Point nosecoef)
        {
            var min = new Point(imgnose.Width, imgnose.Height);
            var max = new Point(0, 0);
            var storage = new MemStorage();
            for (var contours = imgnose.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
            contours != null; contours = contours.HNext)
            {
                var pts = contours.ToArray();
                for (var i = 0; i < pts.Length; i++)
                {
                    if (min.X > pts[i].X) { min.X = pts[i].X; }
                    if (min.Y > pts[i].Y) { min.Y = pts[i].Y; }
                    if (max.X < pts[i].X) { max.X = pts[i].X; }
                    if (max.Y < pts[i].Y) { max.Y = pts[i].Y; }
                }
            }
            max.X += ptnose.X; max.Y += ptnose.Y;
            min.X += ptnose.X;
            min.Y = minY;
            var Rect = new Rectangle(min.X, min.Y, max.X - min.X, max.Y - min.Y);
            ptnose.X = Rect.Left;
            ptnose.Y = Rect.Top;
            nosecoef = new Point(max.X - min.X, max.Y - min.Y);
            return Rect;
        }

        private void FindLimitsX(Image<Gray, byte> img, ref Point min, ref Point max)
        {
            var storage = new MemStorage();
            for (var contours = img.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
            contours != null; contours = contours.HNext)
            {
                var pts = contours.ToArray();
                for (var i = 0; i < pts.Length; i++)
                {
                    if (min.X > pts[i].X)
                    {
                        min = pts[i];
                    }
                    if (max.X < pts[i].X)
                    {
                        max = pts[i];
                    }
                }
            }
        }

        public void separateUpDown(Image<Gray, byte> img, ref List<Point> UP, ref List<Point> Down, ref Point min, ref Point max)
        {
            var storage = new MemStorage();
            FindLimitsX(img, ref min, ref max);
            for (var contours = img.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage); contours != null; contours = contours.HNext)
            {
                var pts = contours.ToArray();
                for (var i = 0; i < pts.Length; i++)
                {
                    var Y = min.Y + (((pts[i].X - min.X) * (max.Y - min.Y)) / (max.X - min.X));
                    if (NotRadiusY(max, min, pts[i], Y, 5))
                        if (pts[i].Y <= Y) UP.Add(pts[i]);
                        else Down.Add(pts[i]);
                }
            }
            UP.Sort(new PointComparer());
            Down.Sort(new PointComparerY());
        }

        private static bool NotRadiusY(Point max, Point min, Point pt, int Y, int radius)
        {
            if ((Math.Abs(pt.Y - Y) < radius) && (Math.Abs(min.X - pt.X) > radius) && (Math.Abs(max.X - pt.X) > radius))
            {
                return false;
            }
            return true;
        }
    }
}
