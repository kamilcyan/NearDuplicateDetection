using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NearDuplicateDetection.HashFunctions;


namespace NearDuplicateDetection
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool goClick = false;
        bool goClickLsh = false;
        bool goClickKnn = false;

        Image loadedImage = new Image();
        Image loadedImageLsh = new Image();
        Image loadedImageKnn = new Image();
        Image comparedImage = new Image();
        Image comparedImageLsh = new Image();
        Image comparedImageKnn = new Image();

        private FeatureExtraction featureExtraction = new FeatureExtraction();
        List<System.Drawing.Point> points = new List<System.Drawing.Point>();
        List<string> directories = new List<string>();
        List<string> directoriesLsh = new List<string>();
        List<string> directoriesKnn = new List<string>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadFirstAverage(object sender, RoutedEventArgs e)
        {
            if (directories.Count > 0)
            {
                directories.RemoveAt(directories.Count - 1);
            }
            int numberOfObjects = LoadedImagePanel.Children.Count;
            if (numberOfObjects > 0)
            {
                for (int i = 0; i < numberOfObjects; i++)
                {
                    LoadedImagePanel.Children.RemoveAt(i);
                }
            }
            int numberOfComparedObjects = ComparedImagePanel.Children.Count;
            if (numberOfComparedObjects > 0)
            {
                for (int i = 0; i < numberOfComparedObjects; i++)
                {
                    ComparedImagePanel.Children.RemoveAt(i);
                }
            }
            LoadFirstPicture();
        }

        //private void Compare(object sender, RoutedEventArgs e)
        //{
        //    float scale = 0;
        //    bool isHeightGreater = false;

        //    if (directories.Count > 1)
        //    {
        //        directories.RemoveAt(directories.Count - 1);
        //    }
        //    int[] dimensions = new int[2];
        //    int height = 0;
        //    int width = 0;
        //    //List<string> results = new List<string>();
        //    List<string> results2 = new List<string>();
        //    if (loadedImage.Source != null)
        //    {

        //        string source = "";
        //        string extension = "";
        //        foreach (var p in points)
        //        {
        //            var ell = new Ellipse();
        //            ell.Width = 4;
        //            ell.Height = 4;
        //            ell.Opacity = 1;
        //            var brush1 = new SolidColorBrush();
        //            brush1.Color = Color.FromRgb(255, 0, 0);
        //            ell.Fill = brush1;
        //            var ell2 = new Ellipse();
        //            ell2.Width = 4;
        //            ell2.Height = 4;
        //            ell2.Opacity = 1;
        //            ell2.Fill = brush1;

        //            BackgroundCanvas.Children.Add(ell);
        //            BackgroundCanvas2.Children.Add(ell2);
        //            Canvas.SetTop(ell, p.X);
        //            Canvas.SetLeft(ell, p.Y);
        //            Canvas.SetTop(ell2, p.X);
        //            Canvas.SetLeft(ell2, p.Y);
        //        }

        //        OpenFileDialog op = new OpenFileDialog();
        //        op.Title = "Select a picture";
        //        op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
        //          "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
        //          "Portable Network Graphic (*.png)|*.png";
        //        if (op.ShowDialog() == true)
        //        {
        //            comparedImage.Source = new BitmapImage(new Uri(op.FileName));
        //            source = comparedImage.Source.ToString();
        //            string substrings = source.Substring(8);
        //            //isHeightGreater = featureExtraction.IsHeightGreater(substrings);
        //            comparedImage.Height = 500;
        //            comparedImage.Width = 500;
        //            //if (isHeightGreater)
        //            //{
        //            //    comparedImage.Height = 500;
        //            //}
        //            //else
        //            //{
        //            //    comparedImage.Width = 500;
        //            //}

        //            ComparedImagePanel.Children.Add(comparedImage);
        //            directories.Add(substrings);
        //        }

        //        BackgroundCanvas.Opacity = 1;
        //        BackgroundCanvas2.Opacity = 1;
        //        MarkingZone.Opacity = 0.3;
        //        MarkingZone2.Opacity = 0.3;

        //        var result = featureExtraction.DoPHash(directories.First(), directories, points);

        //        //results2 = featureExtraction.DoDHash(directories, extension);

        //        var results = FeatureExtraction.WriteResult(result);
        //        //results2 = featureExtraction.DoDHash(directories, extension);
        //        ResultList.TextWrapping = TextWrapping.Wrap;
        //        foreach (var r in results)
        //        {
        //            ResultList.Text += r;
        //        }

        //        foreach (var r in results2)
        //        {
        //            ResultList.Text += r;
        //        }
        //    }

        //}

        private void AllAverage(object sender, RoutedEventArgs e)
        {
            if (goClick)
            {

                int numberOfPoints = 50;
                int numberOfObjects = ComparedImagePanel.Children.Count;
                if (numberOfObjects > 0)
                {
                    for (int i = 0; i < numberOfObjects; i++)
                    {
                        ComparedImagePanel.Children.RemoveAt(i);
                    }
                }
                if (directories.Count > 1)
                {
                    directories.RemoveAt(directories.Count - 1);
                }
                List<string> results2 = new List<string>();

                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                _ = dialog.ShowDialog();


                points = featureExtraction.SetInterestPoints(numberOfPoints);
                bool nameDuplicate = isNameDuplicate.IsChecked == true ? true : false;
                bool gaussianBlur = isGauss.IsChecked == true ? true : false;

                DateTime start = DateTime.Now;
                AverageHash averageHash = new AverageHash();
                goClick = false;
                var result = averageHash.DoPHash(directories.First(), dialog.FileName, ".jpg", nameDuplicate);
                //var result = featureExtraction.DoPHash(directories.First(), dialog.FileName, ".jpg", points, nameDuplicate, gaussianBlur);
                DateTime end = DateTime.Now;
                TimeSpan ts = (end - start);

                //results2 = featureExtraction.DoDHash(directories, extension);

                var results = FeatureExtraction.WriteResult(result);
                //var results = FeatureExtraction.WriteResult(result);


                Newwindow newwindow = new Newwindow();
                newwindow.Show();
                newwindow.ShowResults(results, results2);

                var bestMatch = result.OrderBy(x => x.Value.Max(y => y.match)).Last();



                var left = bestMatch.Key;
                var right = bestMatch.Value.OrderBy(x => x.match).Last();

                comparedImage.Source = new BitmapImage(new Uri(right.otherFile));
                comparedImage.Height = 400;
                comparedImage.Width = 400;
                ComparedImagePanel.Children.Add(comparedImage);

                FillResultList(comparedImage.Source.ToString(), /*stopwatch.ElapsedMilliseconds.ToString()*/ ts.TotalMilliseconds + " milisec");
            }
        }

        private void LoadFirstPicture()
        {
            bool isHeightGreater = false;
            string source = "";
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                loadedImage.Source = new BitmapImage(new Uri(op.FileName));
                source = loadedImage.Source.ToString();
                string substrings = source.Substring(8);
                isHeightGreater = featureExtraction.IsHeightGreater(substrings);
                if (isHeightGreater)
                {
                    loadedImage.Height = 400;
                }
                else
                {
                    loadedImage.Width = 400;
                }
                if (ResultList.Text != "")
                {
                    ResultList.Text = "";
                }



                ResultList.TextWrapping = TextWrapping.Wrap;
                ResultList.Text += "Image:";
                ResultList.Text += "\n";
                ResultList.Text += loadedImage.Source;
                ResultList.Text += "\n";

                LoadedImagePanel.Children.Add(loadedImage);
                directories.Add(substrings);
                goClick = true;
            }
        }

        private void LoadFirstPictureLsh()
        {
            bool isHeightGreater = false;
            string source = "";
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                loadedImageLsh.Source = new BitmapImage(new Uri(op.FileName));
                source = loadedImageLsh.Source.ToString();
                string substrings = source.Substring(8);
                isHeightGreater = featureExtraction.IsHeightGreater(substrings);
                if (isHeightGreater)
                {
                    loadedImageLsh.Height = 400;
                }
                else
                {
                    loadedImageLsh.Width = 400;
                }
                if (ResultListLsh.Text != "")
                {
                    ResultListLsh.Text = "";
                }



                ResultListLsh.TextWrapping = TextWrapping.Wrap;
                ResultListLsh.Text += "Image:";
                ResultListLsh.Text += "\n";
                ResultListLsh.Text += loadedImageLsh.Source;
                ResultListLsh.Text += "\n";

                LoadedImagePanelLsh.Children.Add(loadedImageLsh);
                directoriesLsh.Add(substrings);
                goClickLsh = true;
            }
        }

        private void LoadFirstPictureKnn()
        {
            bool isHeightGreater = false;
            string source = "";
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                loadedImageKnn.Source = new BitmapImage(new Uri(op.FileName));
                source = loadedImageKnn.Source.ToString();
                string substrings = source.Substring(8);
                isHeightGreater = featureExtraction.IsHeightGreater(substrings);
                if (isHeightGreater)
                {
                    loadedImageKnn.Height = 400;
                }
                else
                {
                    loadedImageKnn.Width = 400;
                }
                if (ResultListKnn.Text != "")
                {
                    ResultListKnn.Text = "";
                }



                ResultListKnn.TextWrapping = TextWrapping.Wrap;
                ResultListKnn.Text += "Image:";
                ResultListKnn.Text += "\n";
                ResultListKnn.Text += loadedImageKnn.Source;
                ResultListKnn.Text += "\n";

                LoadedImagePanelKnn.Children.Add(loadedImageKnn);
                directoriesKnn.Add(substrings);
                goClickKnn = true;
            }
        }

        private void FillResultList(string source, string timers)
        {
            ResultList.TextWrapping = TextWrapping.Wrap;
            ResultList.Text += "Best match:";
            ResultList.Text += "\n";
            ResultList.Text += source;

            TimeElapsedMeasure.Text = timers;
        }

        private void GetHash(object sender, RoutedEventArgs e)
        {
            //AverageHash ah = new AverageHash();
            //HashList.Text += ah.Hash((SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>)loadedImage);
        }

        private void LoadFirstLsh(object sender, RoutedEventArgs e)
        {
            if (directoriesLsh.Count > 0)
            {
                directoriesLsh.RemoveAt(directoriesLsh.Count - 1);
            }
            int numberOfObjects = LoadedImagePanelLsh.Children.Count;
            if (numberOfObjects > 0)
            {
                for (int i = 0; i < numberOfObjects; i++)
                {
                    LoadedImagePanelLsh.Children.RemoveAt(i);
                }
            }
            int numberOfComparedObjects = ComparedImagePanelLsh.Children.Count;
            if (numberOfComparedObjects > 0)
            {
                for (int i = 0; i < numberOfComparedObjects; i++)
                {
                    ComparedImagePanelLsh.Children.RemoveAt(i);
                }
            }
            LoadFirstPictureLsh();
        }

        private void AllLsh(object sender, RoutedEventArgs e)
        {

        }

        private void LoadFirstKnn(object sender, RoutedEventArgs e)
        {
            if (directoriesKnn.Count > 0)
            {
                directoriesKnn.RemoveAt(directoriesKnn.Count - 1);
            }
            int numberOfObjects = LoadedImagePanelKnn.Children.Count;
            if (numberOfObjects > 0)
            {
                for (int i = 0; i < numberOfObjects; i++)
                {
                    LoadedImagePanelKnn.Children.RemoveAt(i);
                }
            }
            int numberOfComparedObjects = ComparedImagePanelKnn.Children.Count;
            if (numberOfComparedObjects > 0)
            {
                for (int i = 0; i < numberOfComparedObjects; i++)
                {
                    ComparedImagePanelKnn.Children.RemoveAt(i);
                }
            }
            LoadFirstPictureKnn();
        }

        private void AllKnn(object sender, RoutedEventArgs e)
        {
            if (goClickKnn)
            {

                int numberOfPoints = 50;
                int numberOfObjects = ComparedImagePanelKnn.Children.Count;
                if (numberOfObjects > 0)
                {
                    for (int i = 0; i < numberOfObjects; i++)
                    {
                        ComparedImagePanelKnn.Children.RemoveAt(i);
                    }
                }
                if (directoriesKnn.Count > 1)
                {
                    directoriesKnn.RemoveAt(directoriesKnn.Count - 1);
                }
                List<string> results2 = new List<string>();

                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                _ = dialog.ShowDialog();


                points = featureExtraction.SetInterestPoints(numberOfPoints);
                bool nameDuplicate = isNameDuplicate.IsChecked == true ? true : false;
                bool gaussianBlur = isGauss.IsChecked == true ? true : false;

                DateTime start = DateTime.Now;
                AverageHash averageHash = new AverageHash();
                goClickKnn = false;
                //var result = averageHash.DoPHash(directories.First(), dialog.FileName, ".jpg", nameDuplicate);
                var result = featureExtraction.DoPHash(directoriesKnn.First(), dialog.FileName, ".jpg", points, nameDuplicate, gaussianBlur);
                DateTime end = DateTime.Now;
                TimeSpan ts = (end - start);

                //results2 = featureExtraction.DoDHash(directories, extension);

                var results = FeatureExtraction.WriteResult(result);
                //var results = FeatureExtraction.WriteResult(result);


                Newwindow newwindow = new Newwindow();
                newwindow.Show();
                newwindow.ShowResults(results, results2);

                var bestMatch = result.OrderBy(x => x.Value.Max(y => y.match)).Last();



                var left = bestMatch.Key;
                var right = bestMatch.Value.OrderBy(x => x.match).Last();

                comparedImageKnn.Source = new BitmapImage(new Uri(right.otherFile));
                comparedImageKnn.Height = 400;
                comparedImageKnn.Width = 400;
                ComparedImagePanelKnn.Children.Add(comparedImageKnn);

                FillResultList(comparedImageKnn.Source.ToString(), /*stopwatch.ElapsedMilliseconds.ToString()*/ ts.TotalMilliseconds + " milisec");
            }
        }

        //private void GaussianDifference(object sender, RoutedEventArgs e)
        //{
        //    string source = loadedImage.Source.ToString();
        //    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(source);
        //    FeatureExtraction.ApplyGaussianBlur(ref bmp, 4);
        //    Canvas cv = new Canvas();
        //    Brush brush = (Brush)bmp;
        //    cv.Background = bmp;
        //    LoadedImagePanel.Children.Add(cv);
        //}
    }
}
