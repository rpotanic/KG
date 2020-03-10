﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ЛабРабКомГраф
{
    public partial class Form1 : Form
    {
        Bitmap image;
        public Form1()
        {
            InitializeComponent();
        }

        abstract class Filters
        {
            protected abstract Color calcNewPixelColor(Bitmap im, int x, int y);

            public Bitmap ProcessImage(Bitmap im, BackgroundWorker bw)
            {
                Bitmap res = new Bitmap(im.Width, im.Height);
                for (int i = 0; i < im.Width; i++)
                {
                    bw.ReportProgress((int)((float)i / res.Width * 100));
                    if (bw.CancellationPending)
                        return null;
                    for (int j = 0; j < im.Height; j++)
                    {
                        res.SetPixel(i, j, calcNewPixelColor(im, i, j));
                    }
                }
                return res;
            }

            public int Clamp(int val, int min, int max)
            {
                if (val < min)
                    return min;
                if (val > max)
                    return max;
                return val;
            }
        }

        class InvertFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                Color resColor = Color.FromArgb(255 - sColor.R, 255 - sColor.G, 255 - sColor.B);
                return resColor;
            }
        }

        class GrayScaleFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                int intens = (int)(0.299 * sColor.R + 0.587 * sColor.G + 0.114 * sColor.B);
                Color resColor = Color.FromArgb(intens, intens, intens);
                return resColor;
            }
        }

        class SepiaFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                int k = 50;
                int intens = (int)(0.299 * sColor.R + 0.587 * sColor.G + 0.114 * sColor.B);
                double resR = intens + 2 * k;
                double resG = intens + 0.5 * k;
                double resB = intens - 1 * k;
                Color resColor = Color.FromArgb(Clamp((int)resR, 0, 255), Clamp((int)resG, 0, 255), Clamp((int)resB, 0, 255));
                return resColor;
            }
        }

        class UpBrightnessFilter : Filters
        {
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                Color sColor = im.GetPixel(x, y);
                int k = 50;
                Color resColor = Color.FromArgb(Clamp((int)sColor.R + k, 0, 255), Clamp((int)sColor.G + k, 0, 255), Clamp((int)sColor.B + k, 0, 255));
                return resColor;
            }
        }

        class MatrixFilter : Filters
        {
            protected float[,] ker = null;
            protected MatrixFilter() { }
            public MatrixFilter(float[,] kernel)
            {
                this.ker = kernel;
            }
            protected override Color calcNewPixelColor(Bitmap im, int x, int y)
            {
                int radX = ker.GetLength(0) / 2;
                int radY = ker.GetLength(1) / 2;
                float resR = 0;
                float resG = 0;
                float resB = 0;
                for (int i = -radY; i <= radY; i++)
                    for (int j = -radX; j <= radX; j++)
                    {
                        int idX = Clamp(x + i, 0, im.Width - 1);
                        int idY = Clamp(y + j, 0, im.Height - 1);
                        Color nearColor = im.GetPixel(idX, idY);
                        resR += nearColor.R * ker[j + radX, i + radY];
                        resG += nearColor.G * ker[j + radX, i + radY];
                        resB += nearColor.B * ker[j + radX, i + radY];
                    }
                return Color.FromArgb(Clamp((int)resR, 0, 255), Clamp((int)resG, 0, 255), Clamp((int)resB, 0, 255));
            }
        }

        class BlurFilter : MatrixFilter
        {
            public BlurFilter()
            {
                int sizeX = 3;
                int sizeY = 3;
                ker = new float[sizeX, sizeY];
                for (int i = 0; i < sizeX; i++)
                    for (int j = 0; j < sizeY; j++)
                        ker[i, j] = 1.0f / (float)(sizeY * sizeX);
            }
        }

        class GaussianFilter : MatrixFilter
        {
            public void createGaussianKernel(int rad, float sigma)
            {
                int size = 2 * rad + 1;
                ker = new float[size, size];
                float norm = 0;
                for (int i = -rad; i <= rad; i++)
                    for (int j = -rad; j <= rad; j++)
                    {
                        ker[i + rad, j + rad] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                        norm += ker[i + rad, j + rad];
                    }
                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                        ker[i, j] /= norm;
            }
            public GaussianFilter()
            {
                createGaussianKernel(3, 2);
            }
        }

        class SobelsFilter : MatrixFilter
        {
            public SobelsFilter()
            {
                int sizeX = 3;
                int sizeY = 3;
                ker = new float[sizeX, sizeY];
                ker[0, 0] = -1;
                ker[0, 1] = -2;
                ker[0, 2] = -1;
                ker[1, 0] = 0;
                ker[1, 1] = 0;
                ker[1, 2] = 0;
                ker[2, 0] = 1;
                ker[2, 1] = 2;
                ker[2, 2] = 1;
            }
        }

        class UpSharpnessFilter : MatrixFilter
        {
            public UpSharpnessFilter()
            {
                int sizex = 3;
                int sizey = 3;
                ker = new float[sizex, sizey];
                ker[0, 0] = 0;
                ker[0, 1] = -1;
                ker[0, 2] = 0;
                ker[1, 0] = -1;
                ker[1, 1] = 5;
                ker[1, 2] = -1;
                ker[2, 0] = 0;
                ker[2, 1] = -1;
                ker[2, 2] = 0;
            }
        }

        private void файлToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image files|*.png;*.jpeg;*.jpg;*.bmp*|All files(*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                image = new Bitmap(dialog.FileName);
                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }
        }

        private void инверсияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InvertFilter filter = new InvertFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Bitmap NewIm = ((Filters)e.Argument).ProcessImage(image, backgroundWorker1);
            if (backgroundWorker1.CancellationPending != true)
                image = NewIm;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                pictureBox1.Image = image;
                pictureBox1.Refresh();
            }
            progressBar1.Value = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        private void размытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new BlurFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void гауссовоРазмытиеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GaussianFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void оттенкиСерогоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new GrayScaleFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void сепияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new SepiaFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void увеличитьЯркостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new UpBrightnessFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void собеляToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new SobelsFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }

        private void повыситьРезкостьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Filters filter = new UpSharpnessFilter();
            backgroundWorker1.RunWorkerAsync(filter);
        }
    }
}