using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Drawing.Color;
using Path = System.IO.Path;

namespace Steganography
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum State
        {
            Hiding,
            Filling_With_Zeros
        };

        private string selectedImagePath;

        public MainWindow()
        {
            InitializeComponent();
        }

        public static Bitmap embedText(string text, Bitmap bmp)
        {
            State state = State.Hiding;
            int charIndex = 0;
            int charValue = 0;
            long pixelElementIndex = 0;
            int zeros = 0;
            int R = 0, G = 0, B = 0;

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    Color pixel = bmp.GetPixel(j, i);

                    //ostanek pri deljenju je 0 v primeru sodega ali 1 v primeru lihega števila
                    R = pixel.R - pixel.R % 2;
                    G = pixel.G - pixel.G % 2;
                    B = pixel.B - pixel.B % 2;

                    for (int n = 0; n < 3; n++)
                    {
                        //za vsakih 8 novih bitov
                        if (pixelElementIndex % 8 == 0)
                        {
                            //celoten postopek je zaključen če je na koncu dodanih 8 ničel
                            if (state == State.Filling_With_Zeros && zeros == 8)
                            {
                                //dodaj zadnji pixel
                                if ((pixelElementIndex - 1) % 3 < 2)
                                {
                                    bmp.SetPixel(j, i, Color.FromArgb(R, G, B));
                                }

                                //slika s skritim sporočilom
                                return bmp;
                            }

                            if (charIndex >= text.Length)
                            {
                                // če je celo besedilo skrito, začnem dodajati 8 ničel
                                state = State.Filling_With_Zeros;
                            }
                            else
                            {
                                //premakni se na naslednji znak besedila
                                charValue = text[charIndex++];
                            }
                        }

                        //preveri, kateri RGB element piksla je na vrsti za skrivanje
                        switch (pixelElementIndex % 3)
                        {
                            case 0:
                                {
                                    if (state == State.Hiding)
                                    {
                                        R += charValue % 2;

                                        charValue /= 2;
                                    }
                                }
                                break;
                            case 1:
                                {
                                    if (state == State.Hiding)
                                    {
                                        G += charValue % 2;

                                        charValue /= 2;
                                    }
                                }
                                break;
                            case 2:
                                {
                                    if (state == State.Hiding)
                                    {
                                        B += charValue % 2;

                                        charValue /= 2;
                                    }

                                    bmp.SetPixel(j, i, Color.FromArgb(R, G, B));
                                }
                                break;
                        }

                        pixelElementIndex++;

                        if (state == State.Filling_With_Zeros)
                        {
                            zeros++;
                        }
                    }
                }
            }

            return bmp;
        }

        public static string extractText(Bitmap bmp)
        {
            int colorUnitIndex = 0;
            int charValue = 0;
            string extractedText = String.Empty;

            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width; j++)
                {
                    Color pixel = bmp.GetPixel(j, i);

                    for (int n = 0; n < 3; n++)
                    {
                        switch (colorUnitIndex % 3)
                        {
                            case 0:
                                {
                                    charValue = charValue * 2 + pixel.R % 2;
                                }
                                break;
                            case 1:
                                {
                                    charValue = charValue * 2 + pixel.G % 2;
                                }
                                break;
                            case 2:
                                {
                                    charValue = charValue * 2 + pixel.B % 2;
                                }
                                break;
                        }

                        colorUnitIndex++;

                        if (colorUnitIndex % 8 == 0)
                        {
                            charValue = reverseBits(charValue);

                            if (charValue == 0)
                            {
                                return extractedText;
                            }

                            char c = (char)charValue;

                            extractedText += c.ToString();
                        }
                    }
                }
            }

            return extractedText;
        }

        public static int reverseBits(int n)
        {
            int result = 0;

            for (int i = 0; i < 8; i++)
            {
                result = result * 2 + n % 2;

                n /= 2;
            }

            return result;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png)|*.png;";

            if (openFileDialog.ShowDialog() == true)
            {
                selectedImagePath = openFileDialog.FileName;
                SelectedImage.Source = new BitmapImage(new Uri(selectedImagePath));
                SelectedImageLabel.Text = "Izbrana je slika: " + Path.GetFileName(selectedImagePath);
            }
        }

        private void EncodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedImagePath))
            {
                Bitmap image = new Bitmap(selectedImagePath);

                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Text Files (*.txt)|*.txt";

                if (openFileDialog.ShowDialog() == true)
                {
                    string textFilePath = openFileDialog.FileName;
                    string text = File.ReadAllText(textFilePath);

                    if (text.Length > image.Width * image.Height)
                    {
                        MessageBox.Show("besedilo je predolgo");
                        return;
                    }

                    Bitmap bitmapImage = embedText(text, image);

                    string outputFilePath = Path.GetDirectoryName(selectedImagePath) + "\\SLIKA" + Path.GetFileName(selectedImagePath);
                    bitmapImage.Save(outputFilePath, ImageFormat.Png);
                    TextLabel.Text = "slika je shranjena v SLIKA"+ Path.GetFileName(selectedImagePath);
                }
            }
        }

        private void DecodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedImagePath))
            {
                Bitmap image = new Bitmap(selectedImagePath);
                TextLabel.Text = extractText(image);
            }
        }
    }
}
