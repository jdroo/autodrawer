﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AutoDrawer
{
    public partial class Form1 : Form
    {
        Bitmap imageFile;
        Bitmap image;
        Bitmap processedImage;
        public static Bitmap imagePreview;
        int width;
        int height;
        int startDelay;
        int interval;
        int blackThreshold;
        int transparencyThreshold;
        int[,] pixels;
        ArrayList stack;
        PathIntegers horizontal = new PathIntegers { name = "horizontal", path = 45273618 };
        PathIntegers vertical = new PathIntegers { name = "vertical", path = 27453618 };
        PathIntegers diagonal = new PathIntegers { name = "diagonal", path = 36184527 };
        int pathInt = 12345678;
        bool finished;

        //This is a replacement for Cursor.Position in WinForms
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public Form1()
        {
            InitializeComponent();
            startDelay = (int)startDelayNumeric.Value * 1000;
            blackThreshold = (int)blackThreshNumeric.Value;
            transparencyThreshold = (int)transThreshNumeric.Value;
            interval = Convert.ToInt32(intervalInput.Text);
            interval = int.Parse(intervalInput.Text);
            pathList.Items.Add(horizontal);
            pathList.Items.Add(vertical);
            pathList.Items.Add(diagonal);
            pathList.SetSelected(2, true);
            pathInt = ((PathIntegers)pathList.SelectedItem).path;
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            upload();                        
        }

        private void upload()
        {
            // Show the Open File dialog. If the user clicks OK, load the
            // picture that the user chose.
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                imageFile = new Bitmap(openFileDialog1.FileName);
                pictureBox1.Image = imageFile;
                width = imageFile.Width;
                height = imageFile.Height;
                widthInput.Text = width.ToString();
                heightInput.Text = height.ToString();
                image = imageFile;
                imagePreview = image;
            }
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            clear();
        }

        private void clear()
        {
            // Clear the picture.
            pictureBox1.Image = null;
            imageFile = null;
            image = null;
            imagePreview = null;
        }

        private void widthInput_TextChanged(object sender, EventArgs e)
        {
            // Change in width of image
            try
            {
                width = Convert.ToInt32(widthInput.Text);
                width = int.Parse(widthInput.Text);
                image = resizeImage(imageFile, new Size(width, height));
            }
            catch (Exception)
            {
                width = image.Width;
            }
            pictureBox1.Image = image;
            imagePreview = image;
        }

        private void heightInput_TextChanged(object sender, EventArgs e)
        {
            // Change in height of image
            try
            {
                height = Convert.ToInt32(heightInput.Text);
                height = int.Parse(heightInput.Text);
                image = resizeImage(imageFile, new Size(width, height));
            }
            catch (Exception)
            {
                height = image.Height;
            }
            pictureBox1.Image = image;
            imagePreview = image;
        }

        private void startDelayNumeric_ValueChanged(object sender, EventArgs e)
        {
            // Converts start delayed seconds into miliseconds
            startDelay = (int)startDelayNumeric.Value * 1000;
        }

        private void blackThreshNumeric_ValueChanged(object sender, EventArgs e)
        {
            // Threshold at which a pixel is deemed black
            blackThreshold = (int)blackThreshNumeric.Value;
        }

        private void transThreshNumeric_ValueChanged(object sender, EventArgs e)
        {
            // Threshold at which a pixel is deemed transparent
            transparencyThreshold = (int)transThreshNumeric.Value;
        }

        private void intervalInput_TextChanged(object sender, EventArgs e)
        {
            // Delay interval between the drawing of each pixel
            try
            {
                interval = Convert.ToInt32(intervalInput.Text);
                interval = int.Parse(intervalInput.Text);
            }
            catch (Exception)
            {
                interval = 0;
            }
        }

        private void pathList_SelectedIndexChanged(object sender, EventArgs e)
        {
            pathInt = ((PathIntegers)pathList.SelectedItem).path;
        }

        private void processButton_Click(object sender, EventArgs e)
        {
            // Converts image to grayscale and applies thresholds
            try
            {
                Bitmap greyImage = MakeGrayscale3(image);
                processedImage = scan(greyImage, blackThreshold, transparencyThreshold);
                pictureBox1.Image = processedImage;
                imagePreview = processedImage;
            }
            catch (Exception)
            {
                MessageBox.Show("No image was found", "Image Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void previewButton_Click(object sender, EventArgs e)
        {
            // Opens new window with image preview
            Form2 m = new Form2();
            m.Show();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            // Starts drawing
            try
            {
                Thread.Sleep(startDelay);
                finished = draw();
                Thread.Sleep(100);
                if (finished == true)
                    MessageBox.Show("Drawing Complete", "Done", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                else
                    MessageBox.Show("Drawing halted", "Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            catch (Exception)
            {
                MessageBox.Show("No image was found", "Image Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private Bitmap resizeImage(Bitmap imgToResize, Size size)
        {
            // Resizes image
            return new Bitmap(imgToResize, size);
        }

        private Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
new float[] {.3f, .3f, .3f, 0, 0},
new float[] {.59f, .59f, .59f, 0, 0},
new float[] {.11f, .11f, .11f, 0, 0},
new float[] {0, 0, 0, 1, 0},
new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        private Bitmap scan(Bitmap image, int blackThreshold, int transparencyThreshold)
        {
            // Scans each pixel in image and gives it a black or white value
            pixels = new int[image.Width, image.Height];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    if ((pixel.R < blackThreshold | pixel.G < blackThreshold | pixel.B < blackThreshold) && pixel.A > transparencyThreshold)
                    {
                        image.SetPixel(x, y, Color.Black);
                        pixels[x, y] = 1;
                    }
                    else
                    {
                        image.SetPixel(x, y, Color.White);
                        pixels[x, y] = 0;
                    }
                }
            }
            return image;
        }

        private bool draw()
        {
            Point mouseCenter = Cursor.Position;
            int xorigin = mouseCenter.X - (image.Width / 2);
            int yorigin = mouseCenter.Y - (image.Height / 2);
            int xpos = xorigin;
            int ypos = yorigin;
            bool cont = true;

            // Draw all the areas
            stack = new ArrayList();
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Application.DoEvents();
                    if (ModifierKeys == Keys.Alt)
                        return false;
                    if (pixels[x, y] == 1)
                    {
                        xpos = xorigin + x;
                        ypos = yorigin + y;
                        SetCursorPos(xpos, ypos);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
                        cont = drawArea(stack, x, y, xorigin, yorigin);
                        mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
                    }
                    if (!cont)
                    {
                        resetPixels();
                        return false;
                    }
                }
            }
            resetPixels();            
            return true;
        }

        private bool drawArea(ArrayList stack, int x, int y, int xorigin, int yorigin)
        {
            int[] path = new int[8];
            bool cont;
            path = makePath(pathInt);

            while (true)
            {
                Application.DoEvents();
                if (ModifierKeys == Keys.Alt)
                    return false;
                NOP(interval);
                SetCursorPos(xorigin + x, yorigin + y);
                pixels[x, y] = 2;
                /*
		        +---+---+---+
		        | 1 | 2 | 3 |
		        +---+---+---+
		        | 4 |   | 5 |
		        +---+---+---+
		        | 6 | 7 | 8 |
		        +---+---+---+
                */
                cont = false;
                foreach (int i in Enumerable.Range(0, 7))
                {
                    switch (path[i])
                    {
                        case 1:
                            if ((x > 0) && (y > 0))
                            {
                                if (pixels[x - 1, y - 1] == 1)
                                {
                                    push(stack, x, y);
                                    x -= 1;
                                    y -= 1;
                                    cont = true;
                                }
                            }
                            break;
                        case 2:
                            if (y > 0)
                            {
                                if (pixels[x, y - 1] == 1)
                                {
                                    push(stack, x, y);
                                    y -= 1;
                                    cont = true;
                                }
                            }
                            break;
                        case 3:
                            if ((x > 0) && (y < 0))
                            {
                                if (pixels[x + 1, y - 1] == 1)
                                {
                                    push(stack, x, y);
                                    x += 1;
                                    y += 1;
                                    cont = true;
                                }
                            }
                            break;
                        case 4:
                            if (x > 0)
                            {
                                if (pixels[x - 1, y] == 1)
                                {
                                    push(stack, x, y);
                                    x -= 1;
                                    cont = true;
                                }
                            }
                            break;
                        case 5:
                            if (x < (image.Width - 1))
                            {
                                if (pixels[x + 1, y] == 1)
                                {
                                    push(stack, x, y);
                                    x += 1;
                                    cont = true;
                                }
                            }
                            break;
                        case 6:
                            if ((x < 0) && (y > 0))
                            {
                                if (pixels[x - 1, y + 1] == 1)
                                {
                                    push(stack, x, y);
                                    x -= 1;
                                    y -= 1;
                                    cont = true;
                                }
                            }
                            break;
                        case 7:
                            if (y < (image.Height - 1))
                            {
                                if (pixels[x, y + 1] == 1)
                                {
                                    push(stack, x, y);
                                    y += 1;
                                    cont = true;
                                }
                            }
                            break;
                        case 8:
                            if ((x < (image.Width - 1)) && (y < (image.Height - 1)))
                            {
                                if (pixels[x + 1, y + 1] == 1)
                                {
                                    push(stack, x, y);
                                    x += 1;
                                    y += 1;
                                    cont = true;
                                }
                            }
                            break;
                    }
                }
                if (cont)
                    continue;
                if (!pop(stack, ref x, ref y))
                    break;
            }
            return true;
        }

        private int[] makePath(int pathInt)
        {
            List<int> listOfInts = new List<int>();
            while (pathInt > 0)
            {
                listOfInts.Add(pathInt % 10);
                pathInt /= 10;
            }
            listOfInts.Reverse();
            return listOfInts.ToArray();
        }

        private void push(ArrayList stack, int x, int y)
        {
            stack.Add(new Position { x = x, y = y });
        }

        private bool pop(ArrayList stack, ref int x, ref int y)
        {
            if (stack.Count < 1)
                return false;

            Position pos = (Position)stack[stack.Count - 1];

            x = pos.x;
            y = pos.y;

            stack.Remove(pos);
            return true;
        }

        private void resetPixels()
        {
            // Reset the pixels statuses
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (pixels[x, y] == 2)
                        pixels[x, y] = 1;
                }
            }
        }

        private static void NOP(long durationTicks)
        {
            // Static method to initialize and start stopwatch
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedTicks < durationTicks)
            {

            }
        }
    }

    class Position
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    class PathIntegers
    {
        public string name { get; set; }
        public int path { get; set; }
    }
}
