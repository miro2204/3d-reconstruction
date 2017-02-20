using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using System.IO;

namespace _3D_reconstruction
{
    public class ModelPoints
    {
        private readonly List<Point> points;
        private int[] Zpos;
        private int Value1;
        private int Value2;
        private Image<Bgr, byte> My_Image_prof;
        private static volatile ModelPoints instance;
        private static readonly object syncRoot = new Object();
        public static ModelPoints Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new ModelPoints();
                    }
                }
                return instance;
            }
        }

        public ModelPoints()
        {
            points = new List<Point>();
        }

        private readonly EMGUoperation eo = EMGUoperation.Instance;

        public Point[] CalcFrontNose(Point nosecoef, Point ptnose)
        {
            var read = new StreamReader("my_nose_coef.txt");
            var kM = int.Parse(read.ReadLine());
            var nose = new Point[kM];
            for (var i = 0; i < kM; i++)
            {
                nose[i].X = (int)(nosecoef.X * double.Parse(read.ReadLine()) / 0.348);
                nose[i].Y = (int)(nosecoef.Y * double.Parse(read.ReadLine()) / 0.5);
                nose[i].X += ptnose.X;
                nose[i].Y += ptnose.Y;
                points.Add(nose[i]);
            }
            read.Close();
            return nose;
        }

        public Point[] CalcFrontMouth(Image<Gray, byte> imgmouth, Point pt)
        {
            var Points = new List<Point>();
            var min = new Point(imgmouth.Width, 0);
            var max = new Point(0, 0);
            var libsUP = new List<Point>();
            var libsDown = new List<Point>();

            eo.separateUpDown(imgmouth, ref libsUP, ref libsDown, ref min, ref max);

            double raz = max.X - min.X;
            var read = new StreamReader("my_mouth_coef_up.txt");
            var kM = int.Parse(read.ReadLine());
            var koef = new double[kM];
            for (var i = 0; i < kM; i++)
            {
                koef[i] = double.Parse(read.ReadLine());
                var newX = (int)(koef[i] * raz);
                var newY = min.Y;
                for (var j = 1; j < libsUP.Count; j++)
                {
                    if ((libsUP[j].X >= newX) & (libsUP[j - 1].X <= newX))
                    {
                        newY = libsUP[j].Y;
                        break;
                    }
                }
                var ptn = new Point(newX, newY);
                ptn.X += pt.X; ptn.Y += pt.Y;
                points.Add(ptn);
                Points.Add(ptn);
            }
            read.Close();

            read = new StreamReader("my_mouth_coef_down.txt");
            kM = int.Parse(read.ReadLine());
            koef = new double[kM];
            for (var i = 0; i < kM; i++)
            {
                koef[i] = double.Parse(read.ReadLine());
                var newX = (int)(koef[i] * raz);
                var newY = min.Y;
                for (var j = 1; j < libsDown.Count; j++)
                {
                    if ((libsDown[j].X >= newX) & (libsDown[j - 1].X <= newX))
                    {
                        newY = libsDown[j].Y;
                        break;
                    }
                }
                var ptn = new Point(newX, newY);
                ptn.X += pt.X; ptn.Y += pt.Y;
                Points.Add(ptn);
                points.Add(ptn);
            }
            read.Close();

            read = new StreamReader("my_mouth_coef.txt");
            kM = int.Parse(read.ReadLine());
            koef = new double[kM];
            for (var i = 0; i < kM; i++)
            {
                koef[i] = double.Parse(read.ReadLine());
                var newX = (int)(koef[i] * raz);
                var newY = min.Y;
                var ptn = new Point(newX, newY);
                ptn.X += pt.X; ptn.Y += pt.Y;
                Points.Add(ptn);
                points.Add(ptn);
            }
            read.Close();
            return Points.ToArray();
        }

        public Point[] CalcFrontFase(Point center, Point bodySize)
        {
            var Points = new List<Point>();
            var read1 = new StreamReader("my_fase_coef.txt");
            var kM = int.Parse(read1.ReadLine());
            var body1 = new Point[kM];
            var shX = center.X - bodySize.X / 2;
            var shY = center.Y - bodySize.Y / 2;
            for (var i = 0; i < kM; i++)
            {
                body1[i].X = (int)(bodySize.X * double.Parse(read1.ReadLine())) + shX;
                body1[i].Y = (int)(bodySize.Y * double.Parse(read1.ReadLine())) + shY;
                Points.Add(body1[i]);
                points.Add(body1[i]);
            }
            read1.Close();
            var read = new StreamReader("my_body_coef.txt");
            kM = int.Parse(read.ReadLine());
            var body = new Point[kM * 2];
            double a2 = (bodySize.X / 2) * (bodySize.X / 2);
            double b2 = (bodySize.Y / 2) * (bodySize.Y / 2);
            for (var i = 0; i < kM; i++)
            {
                body[i].Y = body[i + kM].Y = (int)(bodySize.Y * double.Parse(read.ReadLine()));
                double y2 = (body[i].Y - (bodySize.Y / 2) - 1) * (body[i].Y - (bodySize.Y / 2) - 1);
                body[i].X = (int)Math.Sqrt(a2 / b2 * Math.Abs(b2 - y2));
                body[i + kM].X = -(int)Math.Sqrt(a2 / b2 * Math.Abs(b2 - y2));
                body[i].X += center.X;
                body[i].Y += center.Y - (bodySize.Y / 2);
                body[i + kM].X += center.X;
                body[i + kM].Y += center.Y - (bodySize.Y / 2);
                Points.Add(body[i]);
                Points.Add(body[i + kM]);
                points.Add(body[i]);
                points.Add(body[i + kM]);
            }
            read.Close();
            return Points.ToArray();
        }

        public Point[] Findlbow(Image<Gray, byte> imgeye, Point pteye)
        {
            var min = new Point(imgeye.Width, 0);
            var max = new Point(0, 0);
            var brow = new List<Point>();
            var Points = new List<Point>();
            var storage = new MemStorage();
            for (var contours = imgeye.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
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
                    brow.Add(pts[i]);
                }
            }
            brow.Sort(new PointComparer());
            double raz = max.X - min.X;
            var read = new StreamReader("my_brows_coef.txt");
            var kM = int.Parse(read.ReadLine());
            var koef = new double[kM];
            for (var i = 0; i < kM; i++)
            {
                koef[i] = double.Parse(read.ReadLine());
                var newX = (int)(koef[i] * raz) + min.X;
                var newY = min.Y;
                for (var j = 1; j < brow.Count; j++)
                {
                    if ((brow[j].X >= newX) & (brow[j - 1].X <= newX))
                    {
                        newY = brow[j].Y;
                        break;
                    }
                }
                var ptn = new Point(newX, newY);
                ptn.X += pteye.X; ptn.Y += pteye.Y;
                Points.Add(ptn);
                points.Add(ptn);
            }
            read.Close();
            return Points.ToArray();
        }

        public Point[] Findrbow(Image<Gray, byte> imgeye, Point pteye)
        {
            var min = new Point(imgeye.Width, 0);
            var max = new Point(0, 0);
            var brow = new List<Point>();
            var Points = new List<Point>();
            var storage = new MemStorage();
            for (var contours = imgeye.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
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
                    brow.Add(pts[i]);
                }
            }
            brow.Sort(new PointComparer());
            double raz = max.X - min.X;
            var read = new StreamReader("my_brows_coef.txt");
            var kM = int.Parse(read.ReadLine());
            var koef = new double[kM];
            for (var i = 0; i < kM; i++)
            {
                koef[i] = 1.1 - double.Parse(read.ReadLine());
            }
            for (var i = 0; i < kM; i++)
            {
                var newX = (int)(koef[i] * raz) + min.X;
                var newY = min.Y;
                for (var j = 1; j < brow.Count; j++)
                {
                    if ((brow[j].X >= newX) & (brow[j - 1].X <= newX))
                    {
                        newY = brow[j].Y;
                        break;
                    }
                }
                var ptn = new Point(newX, newY);
                ptn.X += pteye.X; ptn.Y += pteye.Y;
                points.Add(ptn);
                Points.Add(ptn);
            }
            read.Close();
            return Points.ToArray();
        }

        public Point[] Findeyes(Image<Gray, byte> imgeye, Point pteye)
        {
            var Points = new List<Point>();
            var min = new Point(imgeye.Width, 0);
            var max = new Point(0, 0);
            var eyeUp = new List<Point>();
            var eyeDown = new List<Point>();

            eo.separateUpDown(imgeye, ref eyeUp, ref eyeDown, ref min, ref max);
            double raz = max.X - min.X;
            var read = new StreamReader("my_leye_coef_up.txt");
            var kM = int.Parse(read.ReadLine());
            var koef = new double[kM];
            for (var i = 0; i < kM; i++)
            {
                koef[i] = double.Parse(read.ReadLine());
                var newX = (int)(koef[i] * raz) + min.X;
                var newY = min.Y;
                for (int j = 1; j < eyeUp.Count; j++)
                {
                    if ((eyeUp[j].X >= newX) & (eyeUp[j - 1].X <= newX))
                    {
                        newY = eyeUp[j].Y;
                        break;
                    }
                }
                var ptn = new Point(newX, newY);
                ptn.X += pteye.X; ptn.Y += pteye.Y;
                Points.Add(ptn);
                points.Add(ptn);
            }
            read.Close();

            read = new StreamReader("my_leye_coef_down.txt");
            kM = int.Parse(read.ReadLine());
            koef = new double[kM];
            for (var i = 0; i < kM; i++)
            {
                koef[i] = double.Parse(read.ReadLine());
                var newX = (int)(koef[i] * raz) + min.X;
                var newY = min.Y;
                for (var j = 1; j < eyeDown.Count; j++)
                {
                    if ((eyeDown[j].X >= newX) & (eyeDown[j - 1].X <= newX))
                    {
                        newY = eyeDown[j].Y;
                        break;
                    }
                }
                var ptn = new Point(newX, newY);
                ptn.X += pteye.X; ptn.Y += pteye.Y;
                Points.Add(ptn);
                points.Add(ptn);
            }
            read.Close();
            return Points.ToArray();
        }

        public void ProfileInit(Image<Bgr, byte> image, int v1, int v2)
        {
            My_Image_prof = image;
            Value1 = v1; Value2 = v2;
            Zpos = new int[points.Count];
        }
        public Image<Gray, byte> CalcProfileEyes()
        {

            var Rect = eo.getRectFromImage("ojoI.xml", My_Image_prof);
            var img3 = My_Image_prof.Copy(Rect);
            var imgeye = eo.CannyImage(img3, Value1, Value2);
            var min = Rect.Width;
            var max = 0;

            var storage = new MemStorage();

            for (var contours = imgeye.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
               contours != null; contours = contours.HNext)
            {
                var pts = contours.ToArray();
                for (var i = 1; i < pts.Length; i++)
                {
                    if (min > pts[i].X) { min = pts[i].X; }
                    if (max < pts[i].X) { max = pts[i].X; }
                }
            }
            min += Rect.Left;
            max += Rect.Left;


            var r = new StreamReader("pointconf\\EyeZl.ptZ");
            var cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var s = int.Parse(r.ReadLine());
                Zpos[s] = min;
            }
            r.Close();
            r = null;

            //right side Z
            var reyePos = Rect.Left + ((Rect.Width) / 3 * 2);
            r = new StreamReader("pointconf\\EyeZr.ptZ");
            cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var s = int.Parse(r.ReadLine());
                Zpos[s] = reyePos;
            }
            r.Close();
            r = null;

            //left  brows Z
            r = new StreamReader("pointconf\\brZl.ptZ");
            cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var s = int.Parse(r.ReadLine());
                Zpos[s] = Rect.Left;
            }
            r.Close();
            r = null;

            float koefeye = reyePos - min;
            r = new StreamReader("pointconf\\EyecoefZ.ptZ");
            cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var cof = float.Parse(r.ReadLine());
                var ptZ = (int)(cof * koefeye) + min;
                var cu = int.Parse(r.ReadLine());
                for (var j = 0; j < cu; j++)
                {
                    var s = int.Parse(r.ReadLine());
                    Zpos[s] = ptZ;
                }

            }
            r.Close();
            r = null;
            return imgeye;
        }
        public Image<Gray, byte> CalcProfileMouth(Image<Gray, byte> imgmouth, Point pt)
        {
            var storage = new MemStorage();
            var mouthregion = new Rectangle(My_Image_prof.Width / 2, pt.Y, My_Image_prof.Width / 2, imgmouth.Height);

            var img_mouth = eo.CannyImage(My_Image_prof.Copy(mouthregion), Value1, Value2);

            var min = img_mouth.Width;
            var max = 0;
            for (var contours = img_mouth.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
              contours != null; contours = contours.HNext)
            {
                var pts = contours.ToArray();
                for (var i = 1; i < pts.Length; i++)
                {
                    if (min > pts[i].X) { min = pts[i].X; }
                    if (max < pts[i].X) { max = pts[i].X; }
                }
            }
            min += mouthregion.Left;
            max += mouthregion.Left;
            //left  Z
            var r = new StreamReader("pointconf\\mouthZl.ptZ");
            var cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var s = int.Parse(r.ReadLine());
                Zpos[s] = min;
            }
            r.Close();
            r = null;
            float koef = max - min;
            r = new StreamReader("pointconf\\mouthCoefZ.ptZ");
            cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var cof = float.Parse(r.ReadLine());
                var ptZ = (int)(cof * koef) + min;
                var cu = int.Parse(r.ReadLine());
                for (var j = 0; j < cu; j++)
                {
                    var s = int.Parse(r.ReadLine());
                    Zpos[s] = ptZ;
                }
            }
            r.Close();
            r = null;
            return img_mouth;
        }
        public void CalcProfileNose()
        {
            /////////////////////////////////////////////////////////////////////////////////////////
            //----------------------------------------NOSE---------------------------------------//
            /////////////////////////////////////////////////////////////////////////////////////////

            Zpos[66] = (Zpos[23] + Zpos[68]) / 2;
            Zpos[67] = (Zpos[23] + Zpos[68]) / 2;
            Zpos[62] = (Zpos[56] + Zpos[57]) / 2;
            Zpos[63] = (Zpos[56] + Zpos[57]) / 2;
            Zpos[58] = 2 * Zpos[57] - Zpos[56];
            Zpos[59] = 2 * Zpos[57] - Zpos[56];
            Zpos[60] = 2 * Zpos[57] - Zpos[56];
            Zpos[61] = 2 * Zpos[57] - Zpos[56];
            Zpos[69] = (Zpos[58] + Zpos[57]) / 2;
            Zpos[70] = (Zpos[58] + Zpos[57]) / 2;
            Zpos[76] = (Zpos[61] + Zpos[71]) / 2;
            Zpos[80] = (Zpos[61] + Zpos[71]) / 2;
            Zpos[18] = (Zpos[5] + Zpos[71]) / 2;

            Zpos[64] = (Zpos[66] + Zpos[55]) / 2;
            Zpos[65] = (Zpos[66] + Zpos[55]) / 2;

            Zpos[20] = (Zpos[5] + Zpos[71]) / 2;

            var r = new StreamReader("pointconf\\last.ptZ");
            var cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var s = int.Parse(r.ReadLine());
                Zpos[s] = Zpos[71];
            }
            r.Close();
            r = null;

            //////////////////////////////////////////////////////////////////////////////////////////////
            Zpos[98] = (Zpos[97] + Zpos[81]) / 2;
        }
        public Image<Gray, byte> CalcProfileFrontLine()
        {
            var Frontline = new List<int>();
            var r = new StreamReader("my_frontZ.txt");
            var cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var s = int.Parse(r.ReadLine());
                Frontline.Add(s);
            }
            r.Close();


            var ptn = new Point[Frontline.Count];
            var k = 0;
            foreach (var i in Frontline)
            {
                ptn[k] = new Point(0, points[i].Y);
                k++;
            }

            var storage = new MemStorage();

            var imgprof = eo.CannyImage(My_Image_prof, Value1, Value2);
            for (var contours = imgprof.FindContours(CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, RETR_TYPE.CV_RETR_LIST, storage);
                contours != null; contours = contours.HNext)
            {
                var pts = contours.ToArray();
                for (var i = 1; i < pts.Length; i++)
                {
                    for (var j = 0; j < ptn.Length; j++)
                    {
                        if ((pts[i].Y >= ptn[j].Y) && (pts[i - 1].Y <= ptn[j].Y))
                            if (ptn[j].X < pts[i].X)
                            {
                                ptn[j].X = (pts[i - 1].X + pts[i].X) / 2;
                            }
                    }
                }
            }


            for (var j = 0; j < ptn.Length; j++)
            {
                Zpos[Frontline[j]] = ptn[j].X;
            }
            return imgprof;
        }
        public int[] getProfile()
        {
            return Zpos;
        }
    }
}
