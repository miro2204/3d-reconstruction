using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Tao.OpenGl;
using Tao.FreeGlut;
using Tao.DevIl;

namespace _3D_reconstruction
{ 
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            AnT.InitializeContexts();
          
        }

        Image<Bgr, Byte> My_Image;
        Image<Bgr, Byte> My_Image_copy;
        Image<Bgr, Byte> My_Image_prof;
        Image<Bgr, Byte> My_Image_copy_prof;
        Image<Gray, byte> imgmouth;
        Image<Gray, byte> imgnose;
        Image<Gray, byte> imgleye;
        Image<Gray, byte> imgreye;
        Image<Gray, byte> imglbrow;
        Image<Gray, byte> imgrbrow;

        int[] Zpos;

        readonly EMGUoperation eo = EMGUoperation.Instance;
        readonly ModelPoints mp = ModelPoints.Instance;

        Point pt; Point ptnose; Point ptleye; Point ptreye; Point ptlbrow; Point ptrbrow;
        Point nosecoef;
        Point bodySize;
        Point center;
        Point fasetop;
        List<Point> Points;

        Point move;

        private void drawtriagles(Point p1, Point p2, Point p3)
        {
            My_Image.Draw(new LineSegment2D(p1, p2), new Bgr(Color.DodgerBlue), 1);
            My_Image.Draw(new LineSegment2D(p2, p3), new Bgr(Color.DodgerBlue), 1);
            My_Image.Draw(new LineSegment2D(p3, p1), new Bgr(Color.DodgerBlue), 1);
        }

        private void drawgrid()
        {
            if (ShowGrid.Checked)
            {
                var r = new StreamReader("my_order.txt");
                var cou = int.Parse(r.ReadLine());
                var p1 = new Point(); 
                var p2 = new Point();
                for (var i = 0; i < cou; i++)
                {
                    var s = int.Parse(r.ReadLine());
                    if (i % 3 == 0)
                        p1 = Points[s];
                    if (i % 3 == 1)
                        p2 = Points[s];
                    if (i % 3 == 2)
                    {
                        var p3 = Points[s];
                        drawtriagles(p1, p2, p3);
                    }
                }
            }
            foreach (var pt in Points)
            {
                My_Image.Draw(new CircleF(pt, 2), new Bgr(Color.Red), 1);
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Points = null;
            Points = new List<Point>();
            button3_Click(sender, e);
            button4_Click_1(sender, e);
            button6_Click_1(sender, e);
            button8_Click(sender, e);
            groupHand.Visible = true;
            if (checkUseProf.Checked)
            {
                button29.Enabled = true;
            }
            else
            {
                button18.Enabled = true;
                button1.Enabled = true;
            }
 
            drawgrid();
        }


        string url;

        private void button17_Click(object sender, EventArgs e)
        {
            try
            {

                var Openfile = new OpenFileDialog();
                if (Openfile.ShowDialog() == DialogResult.OK)
                {
                    url = Openfile.FileName;
                    My_Image = new Image<Bgr, byte>(Openfile.FileName);
                    My_Image_copy = new Image<Bgr, byte>(Openfile.FileName);
                    captureImageBox.Image = My_Image;
                    groupBox6.Enabled = true;
                }
            }
            catch
            {
                MessageBox.Show("Зображення повине бути формату .jpg");
            }
        }

        private void openGlInit()
        {
            Glut.glutInit();
            Glut.glutInitDisplayMode(Glut.GLUT_RGB | Glut.GLUT_DOUBLE | Glut.GLUT_DEPTH);

            // очитка окна 
            Gl.glClearColor(255, 255, 255, 1);

            // установка порта вывода в соотвествии с размерами элемента anT 
            Gl.glViewport(0, 0, AnT.Width, AnT.Height);


            Il.ilInit();
            Il.ilEnable(Il.IL_ORIGIN_SET);

            // настройка проекции 
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Glu.gluPerspective(45, AnT.Width / (float)AnT.Height, 0.1, 200);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            // настройка параметров OpenGL для визуализации 
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_LIGHT0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Points = new List<Point>();
            tabControl1.Controls.Remove(GlobalView);
            openGlInit();
            imageBox8.Width = AnfasView.Width / 2 - 3;
        }

        Point3d[] _body;
        int pointCounts;
        int objectsCount;
        float cof;
        PointF[] textureMatrix;

        private void getFaseOrder()
        {
            FaseOrder = new List<int>();
            var r = new StreamReader("my_order.txt");
            var cou = int.Parse(r.ReadLine());
            for (var i = 0; i < cou; i++)
            {
                var s = int.Parse(r.ReadLine());
                FaseOrder.Add(s);
            }
            Poz = int.Parse(r.ReadLine());
            r.Close();
        }

        private void buildmodel()
        {

            getFaseOrder();
            //  Poz = 0;
            objectsCount = 0;
            textBox4.Text = Poz.ToString();

            var minX = Points[0].X;
            var minY = Points[0].Y;
            var maxX = Points[0].X;
            var maxY = Points[0].Y;
            foreach (var pt in Points)
            {
                if (minX > pt.X) { minX = pt.X; }
                if (minY > pt.Y) { minY = pt.Y; }
                if (maxX < pt.X) { maxX = pt.X; }
                if (maxY < pt.Y) { maxY = pt.Y; }
            }
            var width = maxX - minX;
            float height = maxY - minY;
            var imgH = (float)captureImageBox.Image.Bitmap.Height;
            var imgW = (float)captureImageBox.Image.Bitmap.Width;
            var cofWPre = (minX) / height;
            var cofWSuf = (imgW - maxX) / height;
            var cofHPre = (minY) / imgH;
            var cofHSuf = (imgH - maxY) / imgH;
            if (textBox5.Text != "")
                cofWPre = float.Parse(textBox5.Text);
            else
                textBox5.Text = cofWPre.ToString();
            if (textBox6.Text != "")
                cofWSuf = float.Parse(textBox6.Text);
            else
                textBox6.Text = cofWSuf.ToString();
            if (textBox7.Text != "")
                cofHPre = float.Parse(textBox7.Text);
            else
                textBox7.Text = cofHPre.ToString();
            if (textBox8.Text != "")
                cofHSuf = float.Parse(textBox8.Text);
            else
                textBox8.Text = cofHSuf.ToString();
            cof = width / imgH / 2.0f;
            pointCounts = Points.Count;
            _body = new Point3d[pointCounts];
            textureMatrix = new PointF[pointCounts];
            var count = Points.Count;


            if (checkUseProf.Checked)
            {
                int min = Zpos.Min();
                for (int i = 0; i < count; i++)
                {
                    _body[i] = new Point3d(
                        ((Points[i].X - minX)/imgH) - cof,
                        0.5f - ((Points[i].Y - minY)/imgH),
                        (Zpos[i] - min)/imgH

                        );
                    textureMatrix[i] = new PointF(
                        cofWPre + (Points[i].X - minX)/height,
                        -(cofHPre) - ((Points[i].Y - minY)/imgH));
                }

            }
            else
            {

                var rZ = new StreamReader("my_Z.txt");
                rZ.ReadLine();
                for (var i = 0; i < count; i++)
                {
                    _body[i] = new Point3d(
                        ((Points[i].X - minX)/imgH) - cof,
                        0.5f - ((Points[i].Y - minY)/imgH),
                        float.Parse(rZ.ReadLine())/1.0f
                        );
                    textureMatrix[i] = new PointF(
                        cofWPre + (Points[i].X - minX)/height,
                        -(cofHPre) - ((Points[i].Y - minY)/imgH));
                }
                rZ.Close();
            }
        }

        int rotaX; int rotaY; int oldRotY; int oldRotX;
        int moveX, moveY;
        double zoom = -1;
        int X;
        int Y;
        bool rot;
        bool ATmove;

        private void draw()
        {
            try
            {
                Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
                Gl.glLoadIdentity();
                Gl.glColor3f(0, 0, 0);
                Gl.glPushMatrix();
                Gl.glTranslated(moveX/1000.0, moveY/1000.0, zoom);
                Gl.glRotated(oldRotX + rotaX, 0, 1, 0);
                Gl.glRotated(oldRotY + rotaY, 1, 0, 0);
                if (!checkBox1.Checked)
                {
                    Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
                }
                else
                {
                    Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE);
                    Gl.glLineWidth(2);
                }
                Gl.glEnable(Gl.GL_SMOOTH);
                Gl.glEnable(Gl.GL_TEXTURE_2D);
                Gl.glBindTexture(Gl.GL_TEXTURE_2D, mGlTextureObject);
                Gl.glBegin(Gl.GL_TRIANGLES);
                var k = 1;
                var p1 = new Point3d();
                var p2 = new Point3d();
                var p3 = new Point3d();
                foreach (var i in FaseOrder)
                {
                    //нормалі
                    if (k%3 == 1)
                    {
                        p1 = _body[i];
                    }
                    if (k%3 == 2)
                    {
                        p2 = _body[i];
                    }
                    if (k%3 == 0)
                    {
                        p3 = _body[i];
                        var n1 = (p2.Y - p1.Y)*(p3.Z - p1.Z) - (p2.Y - p1.Y)*(p2.Z - p1.Z);
                        var n2 = (p2.Z - p1.Z)*(p3.X - p1.X) - (p3.Z - p1.Z)*(p2.X - p1.X);
                        var n3 = (p2.X - p1.X)*(p3.Y - p1.Y) - (p3.X - p1.X)*(p2.Y - p1.Y);
                        Gl.glNormal3f(n1, n2, n3);
                    }
                    Gl.glColor3f(0, 0, 0);
                    Gl.glTexCoord2f(textureMatrix[i].X, textureMatrix[i].Y);
                    Gl.glVertex3d(_body[i].X, _body[i].Y, _body[i].Z);
                    k++;
                }
                Gl.glEnd();
                Gl.glDisable(Gl.GL_TEXTURE_2D);
                if (checkPoints.Checked)
                {
                    Gl.glPointSize(3);
                    Gl.glBegin(Gl.GL_POINTS);
                    for (var i = 0; i < pointCounts; i++)
                    {
                        Gl.glColor3f(1.0f, 0, 1.0f);
                        Gl.glVertex3d(_body[i].X, _body[i].Y, _body[i].Z);
                    }
                    Gl.glEnd();
                    Gl.glPointSize(3);
                    Gl.glPointSize(6);
                    Gl.glBegin(Gl.GL_POINTS);
                    Gl.glColor3f(1.0f, 0, 0);
                    Gl.glVertex3d(_body[Poz].X, _body[Poz].Y, _body[Poz].Z);
                    Gl.glEnd();
                }
                Gl.glPopMatrix();
                Gl.glFlush();
                AnT.Invalidate();
            }
            catch
            {
            }

        }

        private void tabPage2_Click(object sender, EventArgs e)
        {
            draw();
        }

        private void AnT_MouseDown(object sender, MouseEventArgs e)
        {
            rotaX = 0;
            rotaY = 0;
            if (e.Button == MouseButtons.Right)
            {
                ATmove = true;
            }
            else
            {
                rot = true;
            }
            X = e.X;
            Y = e.Y;
            draw();
        }

        private void AnT_MouseMove(object sender, MouseEventArgs e)
        {
            if (rot)
            {
                rotaX = e.X - X;
                rotaY = e.Y - Y;
                draw();
            }
            if (ATmove)
            {
                moveX = e.X - X;
                moveY = Y - e.Y;
                draw();
            }
        }
        private void AnT_MouseWheel(object sender, MouseEventArgs e)
        {
            rotaX = 0;
            rotaY = 0;
            var zm = e.Delta;
            if (zm > 0)
            {
                zoom += 0.1;
            }
            else
            {
                zoom -= 0.1;
            }
            draw();
        }

        private void AnT_MouseUp(object sender, MouseEventArgs e)
        {
            rot = false;
            ATmove = false;
            oldRotX += rotaX;
            oldRotY += rotaY;
        }

        private static uint MakeGlTexture(int Format, IntPtr pixels, int w, int h)
        {
            // индетефекатор текстурного объекта
            uint texObject;

            // генерируем текстурный объект
            Gl.glGenTextures(1, out texObject);

            // устанавливаем режим упаковки пикселей
            Gl.glPixelStorei(Gl.GL_UNPACK_ALIGNMENT, 1);

            // создаем привязку к только что созданной текстуре
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, texObject);

            // устанавливаем режим фильтрации и повторения текстуры
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_REPEAT);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_REPEAT);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_REPLACE);

            // создаем RGB или RGBA текстуру
            switch (Format)
            {
                case Gl.GL_RGB:
                    Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, w, h, 0, Gl.GL_RGB, Gl.GL_UNSIGNED_BYTE, pixels);
                    break;

                case Gl.GL_RGBA:
                    Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, w, h, 0, Gl.GL_RGBA, Gl.GL_UNSIGNED_BYTE, pixels);
                    break;
            }

            // возвращаем индетефекатор текстурного объекта

            return texObject;
        }

        // флаг - загружена ли текстура
        public bool _textureIsLoad;

        // имя текстуры
        public string TextureName = "";
        // индефекатор текстуры
        public int ImageId;

        // текстурный объект
        public uint mGlTextureObject;

        private void button18_Click(object sender, EventArgs e)
        {
            // создаем изображение с индификатором imageId
            Il.ilGenImages(1, out ImageId);
            // делаем изображение текущим
            Il.ilBindImage(ImageId);

            if (Il.ilLoadImage(url))
            {
                // если загрузка прошла успешно
                // сохраняем размеры изображения
                var width = Il.ilGetInteger(Il.IL_IMAGE_WIDTH);
                var height = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT);

                // определяем число бит на пиксель
                var bitspp = Il.ilGetInteger(Il.IL_IMAGE_BITS_PER_PIXEL);

                switch (bitspp) // в зависимости оп полученного результата
                {
                    // создаем текстуру используя режим GL_RGB или GL_RGBA
                    case 24:
                        mGlTextureObject = MakeGlTexture(Gl.GL_RGB, Il.ilGetData(), width, height);
                        break;
                    case 32:
                        mGlTextureObject = MakeGlTexture(Gl.GL_RGBA, Il.ilGetData(), width, height);
                        break;
                }

                // активируем флаг, сигнализирующий загрузку текстуры
                _textureIsLoad = true;
                // очищаем память
                Il.ilDeleteImages(1, ref ImageId);
            }
        }

        int Poz;

        readonly List<int> Frontline = new List<int>();

        List<int> FaseOrder;

        private void ShowHand_Click(object sender, EventArgs e)
        {
            if (ShowHand.Checked)
            {
                groupHand.Visible = true;
                groupHand.Dock = DockStyle.Left;
            }
            else groupHand.Visible = false;
        }

        private void showKrok_Click(object sender, EventArgs e)
        {
            if (showKrok.Checked)
            {
                groupKrock.Visible = true;
                groupKrock.Dock = DockStyle.Left;
            }
            else groupKrock.Visible = false;
        }

        private void ShowModelConfig_Click(object sender, EventArgs e)
        {
            if (ShowModelConfig.Checked)
            {
                groupModelConfig.Visible = true;
                groupModelConfig.Dock = DockStyle.Left;
            }
            else groupModelConfig.Visible = false;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            // установка порта вывода в соотвествии с размерами элемента anT 
            Gl.glViewport(0, 0, AnT.Width, AnT.Height);
            // настройка проекции 
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Glu.gluPerspective(45, AnT.Width / (float)AnT.Height, 0.1, 200);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            try { draw(); }
            catch { }
        }

        private void button23_Click(object sender, EventArgs e)
        {
            AnT.Parent = this;
            tabControl1.Visible = false;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }

        private void AnT_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {

                AnT.Parent = ModelView;
                tabControl1.Visible = true;
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
            }
        }

        private void Loadbutton_Click(object sender, EventArgs e)
        {
            try
            {
                var fd = new OpenFileDialog {Filter = "*Файл экспорта обстановки ASCII Autodesk(ASE)|*.ase"};
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    var ogo = new OpenGloperation();
                    ogo.OpenFase(fd.FileName, ref _body, ref textureMatrix, ref url);
                    button18_Click(sender, e);
                    getFaseOrder();
                    tabControl1.SelectedTab = ModelView;
                    draw();
                    groupModelConfig.Enabled = true;
                    button23.Enabled = true;
                    SaveButton.Enabled = true;
                    AnT.Enabled = true;
                }
            }
            catch
            {
                MessageBox.Show("3D модель повина бути формату .ase");
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            try
            {
                var fd = new SaveFileDialog {Filter = "*Файл экспорта обстановки ASCII Autodesk(ASE)|*.ase"};
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    var ogo = new OpenGloperation();
                    ogo.SaveFase(fd.FileName, _body, textureMatrix, url);
                }
            }
            catch
            {
                MessageBox.Show("3D модель не може бути збережена, попробуйте ще раз зберегти");
            }
        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            button2_Click_1(sender, e);
            button5_Click(sender, e);
            button7_Click(sender, e);
            button9_Click(sender, e);
            button16.Enabled = true;
            
        }

        private void button16_Click_1(object sender, EventArgs e)
        {
            Points = null;
            Points = new List<Point>();
            button3_Click(sender, e);
            button4_Click_1(sender, e);
            button6_Click_1(sender, e);
            button8_Click(sender, e);
            groupHand.Enabled = true;
            if (checkUseProf.Checked)
            {
                button29.Enabled = true;
                groupProfile.Enabled = true;
            }
            else
            {
                button18.Enabled = true;
                button1.Enabled = true;
            }
            drawgrid();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            buildmodel();
            tabControl1.SelectedTab = ModelView;
            draw();
            groupModelConfig.Enabled = true;
            button21_Click(sender, e);
            button23.Enabled = true;
            SaveButton.Enabled = true;
            AnT.Enabled = true;

        }

        private void button18_Click_1(object sender, EventArgs e)
        {
            // создаем изображение с индификатором imageId
            Il.ilGenImages(1, out ImageId);
            // делаем изображение текущим
            Il.ilBindImage(ImageId);

            if (Il.ilLoadImage(url))
            {
                // если загрузка прошла успешно
                // сохраняем размеры изображения
                var width = Il.ilGetInteger(Il.IL_IMAGE_WIDTH);
                var height = Il.ilGetInteger(Il.IL_IMAGE_HEIGHT);

                // определяем число бит на пиксель
                var bitspp = Il.ilGetInteger(Il.IL_IMAGE_BITS_PER_PIXEL);

                switch (bitspp) // в зависимости оп полученного результата
                {
                    // создаем текстуру используя режим GL_RGB или GL_RGBA
                    case 24:
                        mGlTextureObject = MakeGlTexture(Gl.GL_RGB, Il.ilGetData(), width, height);
                        break;
                    case 32:
                        mGlTextureObject = MakeGlTexture(Gl.GL_RGBA, Il.ilGetData(), width, height);
                        break;
                }

                // активируем флаг, сигнализирующий загрузку текстуры
                _textureIsLoad = true;
                // очищаем память
                Il.ilDeleteImages(1, ref ImageId);
            }
        }

        private void button12_Click_1(object sender, EventArgs e)
        {
            move.Y -= 2;
            label5.Text = move.X.ToString();
            label6.Text = move.Y.ToString();
            My_Image = My_Image_copy.Copy();
            button9_Click(sender, e);
            button16_Click(sender, e);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            move.X += 2;
            label5.Text = move.X.ToString();
            label6.Text = move.Y.ToString();
            My_Image = My_Image_copy.Copy();
            button9_Click(sender, e);
            button16_Click(sender, e);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            move.Y += 2;
            label5.Text = move.X.ToString();
            label6.Text = move.Y.ToString();
            My_Image = My_Image_copy.Copy();
            button9_Click(sender, e);
            button16_Click(sender, e);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            move.X -= 2;
            label5.Text = move.X.ToString();
            label6.Text = move.Y.ToString();
            My_Image = My_Image_copy.Copy();
            button9_Click(sender, e);
            button16_Click(sender, e);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                My_Image = My_Image_copy.Copy();
                button9_Click(sender, e);
                button16_Click(sender, e);
            }
            catch
            {
                MessageBox.Show("Введите число с запятой");
            }
        }

        private void checkShowGlobal_CheckedChanged(object sender, EventArgs e)
        {
            if (checkShowGlobal.Checked)
            {
                tabControl1.Controls.Add(GlobalView);
                tabControl1.SelectedTab = GlobalView;
            }
            else
            {
                tabControl1.Controls.Remove(GlobalView);
                tabControl1.SelectedTab = AnfasView;
            }
        }

        private void ShowGrid_CheckedChanged(object sender, EventArgs e)
        {
            My_Image = My_Image_copy.Copy();
            drawgrid();
        }

        private void button20_Click_1(object sender, EventArgs e)
        {
            Poz = int.Parse(textBox4.Text);
            Poz -= int.Parse(textBox3.Text);

            if (Poz == -1)
                Poz = _body.Length - 1;
            textBox4.Text = Poz.ToString();

            labelkoord.Text = String.Format("X:{0}\nY:{1}\nZ:{2}", _body[Poz].X, _body[Poz].Y, _body[Poz].Z);
            draw();
        }

        private void button21_Click(object sender, EventArgs e)
        {
            Poz = int.Parse(textBox4.Text);
            Poz += int.Parse(textBox3.Text);
            if (Poz == _body.Length)
                Poz = 0;
            textBox4.Text = Poz.ToString();
            labelkoord.Text = String.Format("X:{0}\nY:{1}\nZ:{2}", _body[Poz].X, _body[Poz].Y, _body[Poz].Z);
            draw();
        }

        private void checkPoints_CheckedChanged(object sender, EventArgs e)
        {
            if (checkPoints.Checked)
            {
                Gl.glDisable(Gl.GL_DEPTH_TEST);
                Gl.glDisable(Gl.GL_LIGHTING);
                Gl.glDisable(Gl.GL_LIGHT0);
            }
            else
            {
                Gl.glEnable(Gl.GL_DEPTH_TEST);
                Gl.glEnable(Gl.GL_LIGHTING);
                Gl.glEnable(Gl.GL_LIGHT0);
            }
            draw();
        }

        private void button24_Click(object sender, EventArgs e)
        {
            try
            {
                float X, Y, Z;
                if (radioButton3.Checked)
                {
                    textBox9.Enabled = true;
                    if (textBox9.Text != "")
                    {
                        Z = float.Parse(textBox9.Text);
                        _body[Poz].Z = Z;
                        draw();
                    }
                }
                else if (radioButton1.Checked)
                {
                    textBox10.Enabled = true;
                    if (textBox10.Text != "")
                    {
                        X = float.Parse(textBox10.Text);
                        _body[Poz].X = X;
                        draw();
                    }
                }
                else if (radioButton2.Checked)
                {
                    textBox11.Enabled = true;
                    if (textBox11.Text != "")
                    {
                        Y = float.Parse(textBox11.Text);
                        _body[Poz].Y = Y;
                        draw();
                    }
                }
                
            }
            catch
            {
                MessageBox.Show("Введите число з запятой");
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            textBox10.Enabled = true;
            textBox11.Enabled = false;
            textBox9.Enabled = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            textBox10.Enabled = false;
            textBox11.Enabled = true;
            textBox9.Enabled = false;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            textBox10.Enabled = false;
            textBox11.Enabled = false;
            textBox9.Enabled = true;
        }

        private void button19_Click(object sender, EventArgs e)
        {
            var i = Poz;
            FaseOrder.Add(i);
            objectsCount++;
            draw();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            var wr = new StreamWriter("my_order.txt");
            wr.WriteLine(FaseOrder.Count);
            foreach (int i in FaseOrder)
            {
                wr.WriteLine(i);
            }
            wr.WriteLine(Poz);
            wr.Close();
        }

        private void button25_Click(object sender, EventArgs e)
        {
            var wr = new StreamWriter("my_Z.txt");
            wr.WriteLine(_body.Length);
            for (int i = 0; i < _body.Length; i++)
            {
                wr.WriteLine(_body[i].Z);
            }
            wr.Close();
        }

        private void button30_Click(object sender, EventArgs e)
        {
            Frontline.Clear();
        }

        private void button27_Click(object sender, EventArgs e)
        {
            var i = Poz;
            Frontline.Add(i);
            draw();
        }

        private void button28_Click(object sender, EventArgs e)
        {
            if (textFileName.Text != "")
            {
                var wr = new StreamWriter(textFileName.Text + ".txt");
                wr.WriteLine(Frontline.Count);
                foreach (int i in Frontline)
                {
                    wr.WriteLine(i);
                }
                wr.Close();
            }
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            var nose = mp.CalcFrontNose(nosecoef, ptnose);
            foreach (var ns in nose)
            {
                Points.Add(ns);
            }
            captureImageBox.Image = My_Image;
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var lefteye = mp.Findeyes(imgleye, ptleye);
            var righteye = mp.Findeyes(imgreye, ptreye);
            foreach (var le in lefteye)
            {
                Points.Add(le);
            }
            foreach (var re in righteye)
            {
                Points.Add(re);
            }
            var leftbrow = mp.Findlbow(imglbrow, ptlbrow);
            var rightbrow = mp.Findrbow(imgrbrow, ptrbrow);
            foreach (var lb in leftbrow)
            {
                Points.Add(lb);
            }
            foreach (var rb in rightbrow)
            {
                Points.Add(rb);
            }
            captureImageBox.Image = My_Image;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            var region = new Rectangle(0, My_Image.Height / 3 * 2, My_Image.Width, My_Image.Height / 3);
            var Image = My_Image.Copy(region);

          

            var Rect = eo.getRectFromImage("haarcascade_mcs_mouth.xml", Image);

            Rect.Offset(0, My_Image.Height / 3 * 2);
            pt.X = Rect.Left;
            pt.Y = Rect.Top;
            captureImageBox.Image = My_Image;

            var img2 = My_Image.Copy(Rect);

            imageBox1.Image = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);

            imgmouth = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var Rect = eo.getRectFromImage("ojoI.xml", My_Image);
            ptleye.X = Rect.Left;
            ptleye.Y = Rect.Top;
            captureImageBox.Image = My_Image;
            var img2 = My_Image.Copy(Rect);
            var Rect2 = new Rectangle(Rect.X, Rect.Y - Rect.Height / 2, Rect.Width, Rect.Height / 3 * 2);
            var img3 = My_Image.Copy(Rect2);
            imglbrow = eo.CannyImage(img3, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            ptlbrow.X = Rect2.Left;
            ptlbrow.Y = Rect2.Top;
            imageBox2.Image = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            imageBox7.Image = eo.CannyImage(img3, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            imgleye = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            var region = new Rectangle(My_Image.Width / 2, 0, My_Image.Width / 2, My_Image.Height / 3 * 2);
            var Image = My_Image.Copy(region);

            Rect = eo.getRectFromImage("ojoD.xml", Image);

            Rect = new Rectangle(Rect.Left + My_Image.Width / 2, Rect.Top, Rect.Width, Rect.Height);
            ptreye.X = Rect.Left;
            ptreye.Y = Rect.Top;
            captureImageBox.Image = My_Image;
            img2 = My_Image.Copy(Rect);
            imageBox3.Image = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            imgreye = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            Rect2 = new Rectangle(Rect.X, Rect.Y - Rect.Height / 2, Rect.Width, Rect.Height / 3 * 2);
            img3 = My_Image.Copy(Rect2);
            imageBox6.Image = eo.CannyImage(img3, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            imgrbrow = eo.CannyImage(img3, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            ptrbrow.X = Rect2.Left;
            ptrbrow.Y = Rect2.Top;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var Rect = eo.getRectFromImage("haarcascade_nose.xml", My_Image);
            ptnose.X = Rect.Left;
            ptnose.Y = Rect.Top;
            captureImageBox.Image = My_Image;
            var img2 = My_Image.Copy(Rect);
            imgnose = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            Rect = eo.findnose(imgnose, (ptleye.Y + ptreye.Y) / 2, ref ptnose, ref nosecoef);
            img2 = My_Image.Copy(Rect);
            imgnose = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            imageBox4.Image = imgnose;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            move.X = int.Parse(label5.Text);
            move.Y = int.Parse(label6.Text);
            var Rect = eo.getRectFromImage("haarcascade_frontalface_alt.xml", My_Image);
            var img2 = My_Image.Copy(Rect);
            imageBox5.Image = eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            eo.CannyImage(img2, (int)numericUpDown1.Value, (int)numericUpDown2.Value);
            center = new Point(Rect.Left + (Rect.Width / 2), Rect.Top + (Rect.Height / 2));
            fasetop = new Point(Rect.Left, Rect.Top);
            center.X += move.X;
            center.Y += move.Y;
            fasetop.X += move.X;
            fasetop.Y += move.Y;
            var kW = double.Parse(textBox1.Text);
            var kH = double.Parse(textBox2.Text);
            bodySize = new Point((int)(Rect.Height * kH), (int)(Rect.Width * kW));
            captureImageBox.Image = My_Image;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var mp = ModelPoints.Instance;
            var mouth = mp.CalcFrontMouth(imgmouth, pt);
            foreach (var mt in mouth)
            {
                Points.Add(mt);
            }
            captureImageBox.Image = My_Image;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var fase = mp.CalcFrontFase(center, bodySize);
            foreach (var fs in fase)
            {
                Points.Add(fs);
            }
            captureImageBox.Image = My_Image;
        }

        private void LoadProfileimage()
        {

            var Openfile = new OpenFileDialog();
            if (Openfile.ShowDialog() == DialogResult.OK)
            {

                My_Image_prof = new Image<Bgr, byte>(Openfile.FileName);
                My_Image_copy_prof = new Image<Bgr, byte>(Openfile.FileName);

                imageBox8.Image = My_Image_prof;
            }
        }

        private void checkUseProf_CheckedChanged(object sender, EventArgs e)
        {
            LoadProfButton.Enabled = checkUseProf.Checked;
            imageBox8.Visible = checkUseProf.Checked;
            if (checkUseProf.Checked)
            {
                LoadProfileimage();
                showGroupProfile.Checked = true;
            }

            else groupProfile.Visible = false;
        }

        private void LoadProfButton_Click(object sender, EventArgs e)
        {
            LoadProfileimage();
        }

        private void button29_Click(object sender, EventArgs e)
        {
            My_Image_prof = My_Image_copy_prof.Copy();


            mp.ProfileInit(My_Image_prof, (int)numericUpDown1.Value, (int)numericUpDown2.Value);

            imageCannyFase.Image = mp.CalcProfileFrontLine();

            imageBox9.Image = mp.CalcProfileEyes();
            imageBox10.Image = mp.CalcProfileMouth(imgmouth, pt);

            mp.CalcProfileNose();

            Zpos = mp.getProfile();

            for (int i = 0; i < Zpos.Length; i++)
            {
                My_Image_prof.Draw(new CircleF(new Point(Zpos[i], Points[i].Y), 2), new Bgr(Color.Blue), 1);
            }
            Zpos[98] = (Zpos[97] + Zpos[81]) / 2;
            imageBox8.Image = My_Image_prof;
            button18.Enabled = true;
            button1.Enabled = true;
        }

    }
}