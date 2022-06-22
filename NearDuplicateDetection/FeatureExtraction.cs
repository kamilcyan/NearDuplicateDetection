using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace NearDuplicateDetection
{
    class FeatureExtraction
    {
        int size = 250;

        internal ConcurrentDictionary<string, List<(string otherFile, double match)>> DoPHash(string source, string directory, string extension, List<System.Drawing.Point> points, bool nameDuplicat = false, bool isGaussianBlur = false)
        {
            var files = Directory.GetFiles(directory).Where(x => x.EndsWith(extension)).ToList();

            return DoPHash(source, files, points, nameDuplicat, isGaussianBlur);
        }

        internal ConcurrentDictionary<string, List<(string otherFile, double match)>> DoPHash(string source, List<string> fileList, List<System.Drawing.Point> points, bool nameDuplicat, bool isGaussianBlur)
        {
            var hashes = new ConcurrentDictionary<string, byte[]>();
            List<string> results = new List<string>();
            var tasks = new List<Task>();
            foreach (var item in fileList)
            {
                var t = new Task(() =>
                {
                    hashes.TryAdd(item, CalculatePHash(item, points, isGaussianBlur).ToArray());
                });
                tasks.Add(t);
                t.Start();
            }

            var tt = new Task(() =>
            {
                hashes.TryAdd(source, CalculatePHash(source, points, isGaussianBlur).ToArray());
            });
            tasks.Add(tt);
            tt.Start();
            Task.WaitAll(tasks.ToArray());

            var result = new ConcurrentDictionary<string, List<(string otherFile, double match)>>();

            result = CalculateMatchRate(source, fileList, hashes, result, nameDuplicat);

            results = WriteResult(result);
            return result;
        }
        internal List<string> DoDHash(List<string> directory, string extension, List<System.Drawing.Point> pois, bool isGaussianBlur)
        {
            //var filePaths = Directory.GetFiles(directory).Where(x => x.Contains(extension) && !x.Contains(".tmp")).ToList();
            var hashes = new ConcurrentDictionary<string, byte[,]>();

            List<string> results = new List<string>();

            var tasks = new List<Task>();
            foreach (var item in directory)
            {
                var t = new Task(() =>
                {
                    hashes.TryAdd(item, CalculateDHash(item, pois, isGaussianBlur));
                    Console.WriteLine(item);
                });
                tasks.Add(t);
                t.Start();
            }
            Task.WaitAll(tasks.ToArray());

            // fromFile - List<ToFile, matchRate>
            var result = new ConcurrentDictionary<string, List<(string otherFile, double match)>>();

            //CalculateMatchRate(directory, hashes, result);

            results = WriteResult(result);
            return results;
        }

        private IEnumerable<byte> CalculatePHash(string item, List<System.Drawing.Point> pois, bool isGaussianBlur)
        {
            Image imgToResize = LoadImage(item);
            int[] dimmensions = SetResolution(imgToResize);
            int sizeX = dimmensions[0];
            int sizeY = dimmensions[1];
            var result = new List<Point>();
            GetImageBytes(imgToResize, result, sizeX, sizeY, pois, isGaussianBlur);

            while (result.Count > 0)
            {
                if (result.First().Value > result.Last().Value)
                {
                    result.RemoveAt(0);
                    result.RemoveAt(result.Count-1);
                    yield return 1;
                }
                else
                {
                    result.RemoveAt(0);
                    result.RemoveAt(result.Count - 1);
                    yield return 0;
                }
            }
        }

        private byte[,] CalculateDHash(string item, List<System.Drawing.Point> pois, bool isGaussianBlur)
        {
            Image imgToResize = LoadImage(item);
            int[] dimmensions = SetResolution(imgToResize);
            int sizeX = dimmensions[0];
            int sizeY = dimmensions[1];
            var result = new byte[sizeX, sizeY];
            var rawValues = new byte[sizeX + 1, sizeY];
            //GetImageBytes(imgToResize, rawValues, sizeX + 1, sizeY, pois, isGaussianBlur);
            using (var g = new Bitmap(sizeX + 1, sizeY))
            {
                for (int y = ((sizeY / 2) - 125); y < ((sizeY / 2) + 125); y++)
                {
                    for (int x = ((sizeX / 2) - 125); x < ((sizeX / 2) + 125); x++)
                    {
                        if (rawValues[x, y] > rawValues[x + 1, y])
                        {
                            result[x, y] = 1;
                        }
                        else
                        {
                            result[x, y] = 0;
                        }
                    }
                }
            }
            return result;
        }

        private ConcurrentDictionary<string, List<(string otherFile, double match)>> CalculateMatchRate(string source, List<string> filePaths, ConcurrentDictionary<string, byte[]> hashes, ConcurrentDictionary<string, List<(string otherFile, double match)>> result, bool nameDuplicat)
        {
            var matches = new List<(string otherFile, double match)>();

            foreach (var secondItem in filePaths)
            {
                if ((new Uri(source).AbsolutePath == new Uri(secondItem).AbsolutePath) && nameDuplicat)
                {
                    continue;
                }
                var inputArray = hashes[source];
                var secondArray = hashes[secondItem];

                int matchCount = 0;

                for (int x = 0; x < secondArray.Length; x++)
                {
                    if (inputArray[x] == secondArray[x])
                    {
                        matchCount++;
                    }
                }
                matches.Add((secondItem, (double)matchCount / (double)(secondArray.Length)));
            }
            result.TryAdd(source, matches);
            return result;
        }

        private void GetImageBytes(System.Drawing.Image img, List<Point> points, int sizeX, int sizeY, List<System.Drawing.Point> pois, bool isGaussianBlur)
        {
            Bitmap bitmap = null;
            Graphics graphics = null;
            try
            {
                bitmap = new Bitmap(sizeX, sizeY);
                graphics = Graphics.FromImage((System.Drawing.Image)bitmap);
                graphics.InterpolationMode = InterpolationMode.Low;
                // Draw image with new width and height  
                graphics.DrawImage(img, 0, 0, sizeX, sizeY);
                if (isGaussianBlur)
                {
                    FeatureExtraction.ApplyGaussianBlur(ref bitmap, 20);
                }

                foreach (var p in pois)
                {
                    if (p.X >= bitmap.Width || p.Y >= bitmap.Height)
                    {
                        Point point = new Point();
                        point.X = p.X;
                        point.Y = p.Y;
                        point.Value = 0;
                        points.Add(point);
                        continue;
                    }
                    else
                    {
                        Point point = new Point();
                        Color c = bitmap.GetPixel(p.X, p.Y);

                        int r = c.R;
                        int g = c.G;
                        int b = c.B;
                        int a = (r + g + b) / 3;
                        point.X = p.X;
                        point.Y = p.Y;
                        point.Value = a;
                        points.Add(point);
                    }
                }
            }
            finally
            {
                graphics.Dispose();
                bitmap.Dispose();
                img.Dispose();
            }
        }

        internal static List<string> WriteResult(ConcurrentDictionary<string, List<(string otherFile, double match)>> result)
        {
            var flatten = new List<(string item, string otherItem, double match)>();
            List<string> results = new List<string>();

            foreach (var item in result)
                foreach (var matched in item.Value)
                {
                    flatten.Add((item.Key, matched.otherFile, matched.match));
                }

            foreach (var item in flatten.Skip(flatten.Count - 100))
            {
                results.Add($"{item.item} -- {item.otherItem} == {item.match}" + "\n");
                //Console.WriteLine($"{item.item} -- {item.otherItem} == {item.match}");
            }
            return results;
        }

        private Image LoadImage(string directory)
        {
            return Image.FromFile(directory);
        }

        private int[] SetResolution(Image imgToResize)
        {
            int sizeY = (int)((float)imgToResize.Height * (imgToResize.Height > imgToResize.Width ? 500.0F / (float)imgToResize.Height : 500.0F / (float)imgToResize.Width));
            int sizeX = (int)((float)imgToResize.Width * (imgToResize.Height > imgToResize.Width ? 500.0F / (float)imgToResize.Height : 500.0F / (float)imgToResize.Width));

            int[] dimmensions = { sizeY, sizeX };
            return dimmensions;
        }

        private int BrightnessNormalisation(int sizeY, int sizeX, byte[,] result, List<System.Drawing.Point> pois)
        {
            var sum = 0;

            int iter = 0;

            foreach (var p in pois)
            {
                if (p.X >= sizeX || p.Y >= sizeY)
                {
                    continue;
                }
                sum += result[p.X, p.Y];
                iter++;
            }

            //for (int y = 0; y < sizeY; y++)
            //{
            //    for (int x = 0; x < sizeX; x++)
            //    {
            //        sum += result[x, y];
            //    }
            //}

            return sum / iter;
        }


        internal List<System.Drawing.Point> SetInterestPoints(int numberOfPoints)
        {
            //Random random = new Random();
            //System.Drawing.Point p = new System.Drawing.Point();

            List<System.Drawing.Point> points = new List<System.Drawing.Point>();

            System.Drawing.Point p1 = new System.Drawing.Point();
            p1.X = 305;
            p1.Y = 300;
            points.Add(p1);
            System.Drawing.Point p2 = new System.Drawing.Point();
            p2.X = 233;
            p2.Y = 349;
            points.Add(p2);
            System.Drawing.Point p3 = new System.Drawing.Point();
            p3.X = 131;
            p3.Y = 196;
            points.Add(p3);
            System.Drawing.Point p4 = new System.Drawing.Point();
            p4.X = 215;
            p4.Y = 243;
            points.Add(p4);
            System.Drawing.Point p5 = new System.Drawing.Point();
            p5.X = 133;
            p5.Y = 330;
            points.Add(p5);
            System.Drawing.Point p6 = new System.Drawing.Point();
            p6.X = 208;
            p6.Y = 289;
            points.Add(p6);
            System.Drawing.Point p7 = new System.Drawing.Point();
            p7.X = 264;
            p7.Y = 200;
            points.Add(p7);
            System.Drawing.Point p8 = new System.Drawing.Point();
            p8.X = 337;
            p8.Y = 352;
            points.Add(p8);
            System.Drawing.Point p9 = new System.Drawing.Point();
            p9.X = 368;
            p9.Y = 234;
            points.Add(p9);
            System.Drawing.Point p10 = new System.Drawing.Point();
            p10.X = 211;
            p10.Y = 361;
            points.Add(p10);
            System.Drawing.Point p11 = new System.Drawing.Point();
            p11.X = 182;
            p11.Y = 127;
            points.Add(p11);
            System.Drawing.Point p12 = new System.Drawing.Point();
            p12.X = 211;
            p12.Y = 199;
            points.Add(p12);
            System.Drawing.Point p13 = new System.Drawing.Point();
            p13.X = 260;
            p13.Y = 232;
            points.Add(p13);
            System.Drawing.Point p14 = new System.Drawing.Point();
            p14.X = 219;
            p14.Y = 230;
            points.Add(p14);
            System.Drawing.Point p15 = new System.Drawing.Point();
            p15.X = 346;
            p15.Y = 340;
            points.Add(p15);
            System.Drawing.Point p16 = new System.Drawing.Point();
            p16.X = 316;
            p16.Y = 193;
            points.Add(p16);
            System.Drawing.Point p17 = new System.Drawing.Point();
            p17.X = 294;
            p17.Y = 134;
            points.Add(p17);
            System.Drawing.Point p18 = new System.Drawing.Point();
            p18.X = 299;
            p18.Y = 315;
            points.Add(p18);
            System.Drawing.Point p19 = new System.Drawing.Point();
            p19.X = 345;
            p19.Y = 243;
            points.Add(p19);
            System.Drawing.Point p20 = new System.Drawing.Point();
            p20.X = 125;
            p20.Y = 223;
            points.Add(p20);
            System.Drawing.Point p21 = new System.Drawing.Point();
            p21.X = 230;
            p21.Y = 240;
            points.Add(p21);
            System.Drawing.Point p22 = new System.Drawing.Point();
            p22.X = 178;
            p22.Y = 370;
            points.Add(p22);
            System.Drawing.Point p23 = new System.Drawing.Point();
            p23.X = 163;
            p23.Y = 339;
            points.Add(p23);
            System.Drawing.Point p24 = new System.Drawing.Point();
            p24.X = 178;
            p24.Y = 317;
            points.Add(p24);
            System.Drawing.Point p25 = new System.Drawing.Point();
            p25.X = 212;
            p25.Y = 312;
            points.Add(p25);
            System.Drawing.Point p26 = new System.Drawing.Point();
            p26.X = 284;
            p26.Y = 229;
            points.Add(p26);
            System.Drawing.Point p27 = new System.Drawing.Point();
            p27.X = 337;
            p27.Y = 293;
            points.Add(p27);
            System.Drawing.Point p28 = new System.Drawing.Point();
            p28.X = 287;
            p28.Y = 302;
            points.Add(p28);
            System.Drawing.Point p29 = new System.Drawing.Point();
            p29.X = 214;
            p29.Y = 159;
            points.Add(p29);
            System.Drawing.Point p30 = new System.Drawing.Point();
            p30.X = 213;
            p30.Y = 272;
            points.Add(p30);
            System.Drawing.Point p31 = new System.Drawing.Point();
            p31.X = 350;
            p31.Y = 359;
            points.Add(p31);
            System.Drawing.Point p32 = new System.Drawing.Point();
            p32.X = 270;
            p32.Y = 167;
            points.Add(p32);
            System.Drawing.Point p33 = new System.Drawing.Point();
            p33.X = 139;
            p33.Y = 139;
            points.Add(p33);
            System.Drawing.Point p34 = new System.Drawing.Point();
            p34.X = 368;
            p34.Y = 254;
            points.Add(p34);
            System.Drawing.Point p35 = new System.Drawing.Point();
            p35.X = 275;
            p35.Y = 146;
            points.Add(p35);
            System.Drawing.Point p36 = new System.Drawing.Point();
            p36.X = 132;
            p36.Y = 250;
            points.Add(p36);
            System.Drawing.Point p37 = new System.Drawing.Point();
            p37.X = 234;
            p37.Y = 361;
            points.Add(p37);
            System.Drawing.Point p38 = new System.Drawing.Point();
            p38.X = 256;
            p38.Y = 316;
            points.Add(p38);
            System.Drawing.Point p39 = new System.Drawing.Point();
            p39.X = 322;
            p39.Y = 211;
            points.Add(p39);
            System.Drawing.Point p40 = new System.Drawing.Point();
            p40.X = 160;
            p40.Y = 295;
            points.Add(p40);
            System.Drawing.Point p41 = new System.Drawing.Point();
            p41.X = 179;
            p41.Y = 275;
            points.Add(p41);
            System.Drawing.Point p42 = new System.Drawing.Point();
            p42.X = 142;
            p42.Y = 159;
            points.Add(p42);
            System.Drawing.Point p43 = new System.Drawing.Point();
            p43.X = 181;
            p43.Y = 212;
            points.Add(p43);
            System.Drawing.Point p44 = new System.Drawing.Point();
            p44.X = 230;
            p44.Y = 126;
            points.Add(p44);
            System.Drawing.Point p45 = new System.Drawing.Point();
            p45.X = 220;
            p45.Y = 370;
            points.Add(p45);
            System.Drawing.Point p46 = new System.Drawing.Point();
            p46.X = 226;
            p46.Y = 311;
            points.Add(p46);
            System.Drawing.Point p47 = new System.Drawing.Point();
            p47.X = 154;
            p47.Y = 226;
            points.Add(p47);
            System.Drawing.Point p48 = new System.Drawing.Point();
            p48.X = 247;
            p48.Y = 245;
            points.Add(p48);
            System.Drawing.Point p49 = new System.Drawing.Point();
            p49.X = 343;
            p49.Y = 136;
            points.Add(p49);
            System.Drawing.Point p50 = new System.Drawing.Point();
            p50.X = 359;
            p50.Y = 148;
            points.Add(p50);


            //for (int i = 0; i < numberOfPoints; i++)
            //{
            //    p.X = random.Next(125, 374);
            //    p.Y = random.Next(125, 374);

            //    points.Add(p);
            //}

            return points;
        }

        //internal int[] PictureSize(string directory)
        //{
        //    int[] dimensions = new int[2];

        //    using (var fileStream = new FileStream(directory, FileMode.Open, FileAccess.Read, FileShare.Read))
        //    {
        //        using (var image = Image.FromStream(fileStream, false, false))
        //        {
        //            dimensions[0] = image.Height;
        //            dimensions[1] = image.Width;
        //        }
        //    }

        //    return dimensions;
        //}

        internal bool IsHeightGreater(string directory)
        {
            bool isGreater = false;
            using (var fileStream = new FileStream(directory, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var image = Image.FromStream(fileStream, false, false))
                {
                    if (image.Height > image.Width)
                    {
                        isGreater = true;
                    }
                    else if (image.Height < image.Width)
                    {
                        isGreater = false;
                    }
                }
            }

            return isGreater;
        }

        public static void ApplyGaussianBlur(ref Bitmap bmp, int Weight)
        {
            ConvolutionMatrix m = new ConvolutionMatrix();
            m.Apply(1);
            m.Pixel = Weight;
            m.TopMid = m.MidLeft = m.MidRight = m.BottomMid = 2;
            m.Factor = Weight + 12;

            Convolution C = new Convolution();
            C.Matrix = m;
            C.Convolution3x3(ref bmp);
        }

        //internal float PictureScaling(string directory)
        //{
        //    float scale = 1;
        //    float dimmension = 500;

        //    using (var fileStream = new FileStream(directory, FileMode.Open, FileAccess.Read, FileShare.Read))
        //    {
        //        using (var image = Image.FromStream(fileStream, false, false))
        //        {
        //            if (image.Height > image.Width)
        //            {
        //                scale = dimmension / image.Height;
        //            }
        //            else if (image.Height < image.Width)
        //            {
        //                scale = dimmension / image.Width;
        //            }
        //            else
        //            {
        //                scale = dimmension / image.Height;
        //            }
        //        }
        //    }

        //    return scale;
        //}

        //internal float PictureScaling(Image image, int dimmension)
        //{

        //    if (image.Height > image.Width)
        //    {
        //        scaleImage = (float)dimmension / (float)image.Height;
        //    }
        //    else if (image.Height < image.Width)
        //    {
        //        scaleImage = (float)dimmension / (float)image.Width;
        //    }
        //    else
        //    {
        //        scaleImage = (float)dimmension / (float)image.Height;
        //    }
        //    return scaleImage;
        //}

        //public static Bitmap ResizeImage(Image image, int width, int height)
        //{
        //    var destRect = new Rectangle(0, 0, width, height);
        //    var destImage = new Bitmap(width, height);

        //    destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        //    using (var graphics = Graphics.FromImage(destImage))
        //    {
        //        graphics.CompositingMode = CompositingMode.SourceCopy;
        //        graphics.CompositingQuality = CompositingQuality.HighQuality;
        //        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //        graphics.SmoothingMode = SmoothingMode.HighQuality;
        //        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        //        using (var wrapMode = new ImageAttributes())
        //        {
        //            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        //            graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
        //        }
        //    }

        //    return destImage;
        //}

        //private float HeightToWidthRatio(Image image)
        //{
        //    float height = (float)image.Height;
        //    float width = (float)image.Width;
        //    float ratio = height / width;
        //    return ratio;
        //}

        //private float WidthToHeightRatio(float height, float width)
        //{
        //    float ratio = width / height;
        //    return ratio;
        //}
    }
}
