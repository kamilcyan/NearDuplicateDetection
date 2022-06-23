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

            //var reducedHash = ComparisonExtensions.Reduce(hashes);
            result = CalculateMatchRate(source, fileList, hashes, result, nameDuplicat);

            results = WriteResult(result);
            return result;
        }
        //internal List<string> DoDHash(List<string> directory, string extension, List<System.Drawing.Point> pois, bool isGaussianBlur)
        //{
        //    //var filePaths = Directory.GetFiles(directory).Where(x => x.Contains(extension) && !x.Contains(".tmp")).ToList();
        //    var hashes = new ConcurrentDictionary<string, byte[,]>();

        //    List<string> results = new List<string>();

        //    var tasks = new List<Task>();
        //    foreach (var item in directory)
        //    {
        //        var t = new Task(() =>
        //        {
        //            hashes.TryAdd(item, CalculateDHash(item, pois, isGaussianBlur));
        //            Console.WriteLine(item);
        //        });
        //        tasks.Add(t);
        //        t.Start();
        //    }
        //    Task.WaitAll(tasks.ToArray());

        //    // fromFile - List<ToFile, matchRate>
        //    var result = new ConcurrentDictionary<string, List<(string otherFile, double match)>>();

        //    //CalculateMatchRate(directory, hashes, result);

        //    results = WriteResult(result);
        //    return results;
        //}

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

        //private byte[,] CalculateDHash(string item, List<System.Drawing.Point> pois, bool isGaussianBlur)
        //{
        //    Image imgToResize = LoadImage(item);
        //    int[] dimmensions = SetResolution(imgToResize);
        //    int sizeX = dimmensions[0];
        //    int sizeY = dimmensions[1];
        //    var result = new byte[sizeX, sizeY];
        //    var rawValues = new byte[sizeX + 1, sizeY];
        //    //GetImageBytes(imgToResize, rawValues, sizeX + 1, sizeY, pois, isGaussianBlur);
        //    using (var g = new Bitmap(sizeX + 1, sizeY))
        //    {
        //        for (int y = ((sizeY / 2) - 125); y < ((sizeY / 2) + 125); y++)
        //        {
        //            for (int x = ((sizeX / 2) - 125); x < ((sizeX / 2) + 125); x++)
        //            {
        //                if (rawValues[x, y] > rawValues[x + 1, y])
        //                {
        //                    result[x, y] = 1;
        //                }
        //                else
        //                {
        //                    result[x, y] = 0;
        //                }
        //            }
        //        }
        //    }
        //    return result;
        //}

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
            p1.X = 254;
            p1.Y = 179;
            points.Add(p1);
            System.Drawing.Point p2 = new System.Drawing.Point();
            p2.X = 153;
            p2.Y = 168;
            points.Add(p2);
            System.Drawing.Point p3 = new System.Drawing.Point();
            p3.X = 137;
            p3.Y = 366;
            points.Add(p3);
            System.Drawing.Point p4 = new System.Drawing.Point();
            p4.X = 220;
            p4.Y = 339;
            points.Add(p4);
            System.Drawing.Point p5 = new System.Drawing.Point();
            p5.X = 314;
            p5.Y = 348;
            points.Add(p5);
            System.Drawing.Point p6 = new System.Drawing.Point();
            p6.X = 150;
            p6.Y = 366;
            points.Add(p6);
            System.Drawing.Point p7 = new System.Drawing.Point();
            p7.X = 224;
            p7.Y = 253;
            points.Add(p7);
            System.Drawing.Point p8 = new System.Drawing.Point();
            p8.X = 369;
            p8.Y = 254;
            points.Add(p8);
            System.Drawing.Point p9 = new System.Drawing.Point();
            p9.X = 299;
            p9.Y = 241;
            points.Add(p9);
            System.Drawing.Point p10 = new System.Drawing.Point();
            p10.X = 132;
            p10.Y = 207;
            points.Add(p10);
            System.Drawing.Point p11 = new System.Drawing.Point();
            p11.X = 326;
            p11.Y = 250;
            points.Add(p11);
            System.Drawing.Point p12 = new System.Drawing.Point();
            p12.X = 158;
            p12.Y = 204;
            points.Add(p12);
            System.Drawing.Point p13 = new System.Drawing.Point();
            p13.X = 293;
            p13.Y = 292;
            points.Add(p13);
            System.Drawing.Point p14 = new System.Drawing.Point();
            p14.X = 188;
            p14.Y = 209;
            points.Add(p14);
            System.Drawing.Point p15 = new System.Drawing.Point();
            p15.X = 242;
            p15.Y = 229;
            points.Add(p15);
            System.Drawing.Point p16 = new System.Drawing.Point();
            p16.X = 269;
            p16.Y = 191;
            points.Add(p16);
            System.Drawing.Point p17 = new System.Drawing.Point();
            p17.X = 274;
            p17.Y = 146;
            points.Add(p17);
            System.Drawing.Point p18 = new System.Drawing.Point();
            p18.X = 351;
            p18.Y = 156;
            points.Add(p18);
            System.Drawing.Point p19 = new System.Drawing.Point();
            p19.X = 168;
            p19.Y = 140;
            points.Add(p19);
            System.Drawing.Point p20 = new System.Drawing.Point();
            p20.X = 289;
            p20.Y = 275;
            points.Add(p20);
            System.Drawing.Point p21 = new System.Drawing.Point();
            p21.X = 355;
            p21.Y = 327;
            points.Add(p21);
            System.Drawing.Point p22 = new System.Drawing.Point();
            p22.X = 366;
            p22.Y = 230;
            points.Add(p22);
            System.Drawing.Point p23 = new System.Drawing.Point();
            p23.X = 267;
            p23.Y = 362;
            points.Add(p23);
            System.Drawing.Point p24 = new System.Drawing.Point();
            p24.X = 341;
            p24.Y = 191;
            points.Add(p24);
            System.Drawing.Point p25 = new System.Drawing.Point();
            p25.X = 343;
            p25.Y = 207;
            points.Add(p25);
            System.Drawing.Point p26 = new System.Drawing.Point();
            p26.X = 270;
            p26.Y = 252;
            points.Add(p26);
            System.Drawing.Point p27 = new System.Drawing.Point();
            p27.X = 183;
            p27.Y = 166;
            points.Add(p27);
            System.Drawing.Point p28 = new System.Drawing.Point();
            p28.X = 300;
            p28.Y = 128;
            points.Add(p28);
            System.Drawing.Point p29 = new System.Drawing.Point();
            p29.X = 145;
            p29.Y = 323;
            points.Add(p29);
            System.Drawing.Point p30 = new System.Drawing.Point();
            p30.X = 248;
            p30.Y = 218;
            points.Add(p30);
            System.Drawing.Point p31 = new System.Drawing.Point();
            p31.X = 303;
            p31.Y = 136;
            points.Add(p31);
            System.Drawing.Point p32 = new System.Drawing.Point();
            p32.X = 221;
            p32.Y = 210;
            points.Add(p32);
            System.Drawing.Point p33 = new System.Drawing.Point();
            p33.X = 204;
            p33.Y = 332;
            points.Add(p33);
            System.Drawing.Point p34 = new System.Drawing.Point();
            p34.X = 217;
            p34.Y = 202;
            points.Add(p34);
            System.Drawing.Point p35 = new System.Drawing.Point();
            p35.X = 276;
            p35.Y = 338;
            points.Add(p35);
            System.Drawing.Point p36 = new System.Drawing.Point();
            p36.X = 211;
            p36.Y = 284;
            points.Add(p36);
            System.Drawing.Point p37 = new System.Drawing.Point();
            p37.X = 325;
            p37.Y = 230;
            points.Add(p37);
            System.Drawing.Point p38 = new System.Drawing.Point();
            p38.X = 226;
            p38.Y = 372;
            points.Add(p38);
            System.Drawing.Point p39 = new System.Drawing.Point();
            p39.X = 258;
            p39.Y = 302;
            points.Add(p39);
            System.Drawing.Point p40 = new System.Drawing.Point();
            p40.X = 310;
            p40.Y = 304;
            points.Add(p40);
            System.Drawing.Point p41 = new System.Drawing.Point();
            p41.X = 325;
            p41.Y = 370;
            points.Add(p41);
            System.Drawing.Point p42 = new System.Drawing.Point();
            p42.X = 239;
            p42.Y = 160;
            points.Add(p42);
            System.Drawing.Point p43 = new System.Drawing.Point();
            p43.X = 332;
            p43.Y = 141;
            points.Add(p43);
            System.Drawing.Point p44 = new System.Drawing.Point();
            p44.X = 132;
            p44.Y = 233;
            points.Add(p44);
            System.Drawing.Point p45 = new System.Drawing.Point();
            p45.X = 219;
            p45.Y = 347;
            points.Add(p45);
            System.Drawing.Point p46 = new System.Drawing.Point();
            p46.X = 135;
            p46.Y = 218;
            points.Add(p46);
            System.Drawing.Point p47 = new System.Drawing.Point();
            p47.X = 265;
            p47.Y = 196;
            points.Add(p47);
            System.Drawing.Point p48 = new System.Drawing.Point();
            p48.X = 346;
            p48.Y = 344;
            points.Add(p48);
            System.Drawing.Point p49 = new System.Drawing.Point();
            p49.X = 230;
            p49.Y = 280;
            points.Add(p49);
            System.Drawing.Point p50 = new System.Drawing.Point();
            p50.X = 151;
            p50.Y = 308;
            points.Add(p50);
            System.Drawing.Point p51 = new System.Drawing.Point();
            p51.X = 270;
            p51.Y = 263;
            points.Add(p51);
            System.Drawing.Point p52 = new System.Drawing.Point();
            p52.X = 289;
            p52.Y = 130;
            points.Add(p52);
            System.Drawing.Point p53 = new System.Drawing.Point();
            p53.X = 370;
            p53.Y = 360;
            points.Add(p53);
            System.Drawing.Point p54 = new System.Drawing.Point();
            p54.X = 301;
            p54.Y = 327;
            points.Add(p54);
            System.Drawing.Point p55 = new System.Drawing.Point();
            p55.X = 313;
            p55.Y = 302;
            points.Add(p55);
            System.Drawing.Point p56 = new System.Drawing.Point();
            p56.X = 243;
            p56.Y = 217;
            points.Add(p56);
            System.Drawing.Point p57 = new System.Drawing.Point();
            p57.X = 137;
            p57.Y = 317;
            points.Add(p57);
            System.Drawing.Point p58 = new System.Drawing.Point();
            p58.X = 266;
            p58.Y = 306;
            points.Add(p58);
            System.Drawing.Point p59 = new System.Drawing.Point();
            p59.X = 270;
            p59.Y = 186;
            points.Add(p59);
            System.Drawing.Point p60 = new System.Drawing.Point();
            p60.X = 251;
            p60.Y = 188;
            points.Add(p60);
            System.Drawing.Point p61 = new System.Drawing.Point();
            p61.X = 324;
            p61.Y = 357;
            points.Add(p61);
            System.Drawing.Point p62 = new System.Drawing.Point();
            p62.X = 357;
            p62.Y = 302;
            points.Add(p62);
            System.Drawing.Point p63 = new System.Drawing.Point();
            p63.X = 327;
            p63.Y = 366;
            points.Add(p63);
            System.Drawing.Point p64 = new System.Drawing.Point();
            p64.X = 143;
            p64.Y = 253;
            points.Add(p64);
            System.Drawing.Point p65 = new System.Drawing.Point();
            p65.X = 258;
            p65.Y = 256;
            points.Add(p65);
            System.Drawing.Point p66 = new System.Drawing.Point();
            p66.X = 266;
            p66.Y = 351;
            points.Add(p66);
            System.Drawing.Point p67 = new System.Drawing.Point();
            p67.X = 276;
            p67.Y = 126;
            points.Add(p67);
            System.Drawing.Point p68 = new System.Drawing.Point();
            p68.X = 159;
            p68.Y = 187;
            points.Add(p68);
            System.Drawing.Point p69 = new System.Drawing.Point();
            p69.X = 206;
            p69.Y = 234;
            points.Add(p69);
            System.Drawing.Point p70 = new System.Drawing.Point();
            p70.X = 163;
            p70.Y = 345;
            points.Add(p70);
            System.Drawing.Point p71 = new System.Drawing.Point();
            p71.X = 213;
            p71.Y = 179;
            points.Add(p71);
            System.Drawing.Point p72 = new System.Drawing.Point();
            p72.X = 293;
            p72.Y = 291;
            points.Add(p72);
            System.Drawing.Point p73 = new System.Drawing.Point();
            p73.X = 228;
            p73.Y = 292;
            points.Add(p73);
            System.Drawing.Point p74 = new System.Drawing.Point();
            p74.X = 206;
            p74.Y = 322;
            points.Add(p74);
            System.Drawing.Point p75 = new System.Drawing.Point();
            p75.X = 303;
            p75.Y = 165;
            points.Add(p75);
            System.Drawing.Point p76 = new System.Drawing.Point();
            p76.X = 198;
            p76.Y = 169;
            points.Add(p76);
            System.Drawing.Point p77 = new System.Drawing.Point();
            p77.X = 153;
            p77.Y = 336;
            points.Add(p77);
            System.Drawing.Point p78 = new System.Drawing.Point();
            p78.X = 358;
            p78.Y = 286;
            points.Add(p78);
            System.Drawing.Point p79 = new System.Drawing.Point();
            p79.X = 279;
            p79.Y = 360;
            points.Add(p79);
            System.Drawing.Point p80 = new System.Drawing.Point();
            p80.X = 176;
            p80.Y = 128;
            points.Add(p80);
            System.Drawing.Point p81 = new System.Drawing.Point();
            p81.X = 342;
            p81.Y = 172;
            points.Add(p81);
            System.Drawing.Point p82 = new System.Drawing.Point();
            p82.X = 194;
            p82.Y = 182;
            points.Add(p82);
            System.Drawing.Point p83 = new System.Drawing.Point();
            p83.X = 160;
            p83.Y = 266;
            points.Add(p83);
            System.Drawing.Point p84 = new System.Drawing.Point();
            p84.X = 315;
            p84.Y = 135;
            points.Add(p84);
            System.Drawing.Point p85 = new System.Drawing.Point();
            p85.X = 283;
            p85.Y = 204;
            points.Add(p85);
            System.Drawing.Point p86 = new System.Drawing.Point();
            p86.X = 225;
            p86.Y = 160;
            points.Add(p86);
            System.Drawing.Point p87 = new System.Drawing.Point();
            p87.X = 147;
            p87.Y = 280;
            points.Add(p87);
            System.Drawing.Point p88 = new System.Drawing.Point();
            p88.X = 348;
            p88.Y = 270;
            points.Add(p88);
            System.Drawing.Point p89 = new System.Drawing.Point();
            p89.X = 189;
            p89.Y = 190;
            points.Add(p89);
            System.Drawing.Point p90 = new System.Drawing.Point();
            p90.X = 199;
            p90.Y = 160;
            points.Add(p90);
            System.Drawing.Point p91 = new System.Drawing.Point();
            p91.X = 285;
            p91.Y = 195;
            points.Add(p91);
            System.Drawing.Point p92 = new System.Drawing.Point();
            p92.X = 324;
            p92.Y = 217;
            points.Add(p92);
            System.Drawing.Point p93 = new System.Drawing.Point();
            p93.X = 182;
            p93.Y = 222;
            points.Add(p93);
            System.Drawing.Point p94 = new System.Drawing.Point();
            p94.X = 322;
            p94.Y = 313;
            points.Add(p94);
            System.Drawing.Point p95 = new System.Drawing.Point();
            p95.X = 142;
            p95.Y = 246;
            points.Add(p95);
            System.Drawing.Point p96 = new System.Drawing.Point();
            p96.X = 281;
            p96.Y = 219;
            points.Add(p96);
            System.Drawing.Point p97 = new System.Drawing.Point();
            p97.X = 182;
            p97.Y = 160;
            points.Add(p97);
            System.Drawing.Point p98 = new System.Drawing.Point();
            p98.X = 128;
            p98.Y = 165;
            points.Add(p98);
            System.Drawing.Point p99 = new System.Drawing.Point();
            p99.X = 359;
            p99.Y = 236;
            points.Add(p99);
            System.Drawing.Point p100 = new System.Drawing.Point();
            p100.X = 255;
            p100.Y = 336;
            points.Add(p100);
            System.Drawing.Point p101 = new System.Drawing.Point();
            p101.X = 351;
            p101.Y = 195;
            points.Add(p101);
            System.Drawing.Point p102 = new System.Drawing.Point();
            p102.X = 163;
            p102.Y = 224;
            points.Add(p102);
            System.Drawing.Point p103 = new System.Drawing.Point();
            p103.X = 313;
            p103.Y = 162;
            points.Add(p103);
            System.Drawing.Point p104 = new System.Drawing.Point();
            p104.X = 146;
            p104.Y = 247;
            points.Add(p104);
            System.Drawing.Point p105 = new System.Drawing.Point();
            p105.X = 361;
            p105.Y = 213;
            points.Add(p105);
            System.Drawing.Point p106 = new System.Drawing.Point();
            p106.X = 222;
            p106.Y = 213;
            points.Add(p106);
            System.Drawing.Point p107 = new System.Drawing.Point();
            p107.X = 286;
            p107.Y = 141;
            points.Add(p107);
            System.Drawing.Point p108 = new System.Drawing.Point();
            p108.X = 217;
            p108.Y = 271;
            points.Add(p108);
            System.Drawing.Point p109 = new System.Drawing.Point();
            p109.X = 222;
            p109.Y = 350;
            points.Add(p109);
            System.Drawing.Point p110 = new System.Drawing.Point();
            p110.X = 373;
            p110.Y = 312;
            points.Add(p110);
            System.Drawing.Point p111 = new System.Drawing.Point();
            p111.X = 317;
            p111.Y = 126;
            points.Add(p111);
            System.Drawing.Point p112 = new System.Drawing.Point();
            p112.X = 366;
            p112.Y = 161;
            points.Add(p112);
            System.Drawing.Point p113 = new System.Drawing.Point();
            p113.X = 296;
            p113.Y = 130;
            points.Add(p113);
            System.Drawing.Point p114 = new System.Drawing.Point();
            p114.X = 352;
            p114.Y = 361;
            points.Add(p114);
            System.Drawing.Point p115 = new System.Drawing.Point();
            p115.X = 276;
            p115.Y = 307;
            points.Add(p115);
            System.Drawing.Point p116 = new System.Drawing.Point();
            p116.X = 284;
            p116.Y = 326;
            points.Add(p116);
            System.Drawing.Point p117 = new System.Drawing.Point();
            p117.X = 309;
            p117.Y = 236;
            points.Add(p117);
            System.Drawing.Point p118 = new System.Drawing.Point();
            p118.X = 182;
            p118.Y = 215;
            points.Add(p118);
            System.Drawing.Point p119 = new System.Drawing.Point();
            p119.X = 156;
            p119.Y = 224;
            points.Add(p119);
            System.Drawing.Point p120 = new System.Drawing.Point();
            p120.X = 278;
            p120.Y = 144;
            points.Add(p120);
            System.Drawing.Point p121 = new System.Drawing.Point();
            p121.X = 200;
            p121.Y = 200;
            points.Add(p121);
            System.Drawing.Point p122 = new System.Drawing.Point();
            p122.X = 325;
            p122.Y = 303;
            points.Add(p122);
            System.Drawing.Point p123 = new System.Drawing.Point();
            p123.X = 149;
            p123.Y = 192;
            points.Add(p123);
            System.Drawing.Point p124 = new System.Drawing.Point();
            p124.X = 307;
            p124.Y = 166;
            points.Add(p124);
            System.Drawing.Point p125 = new System.Drawing.Point();
            p125.X = 316;
            p125.Y = 230;
            points.Add(p125);
            System.Drawing.Point p126 = new System.Drawing.Point();
            p126.X = 317;
            p126.Y = 134;
            points.Add(p126);
            System.Drawing.Point p127 = new System.Drawing.Point();
            p127.X = 237;
            p127.Y = 317;
            points.Add(p127);
            System.Drawing.Point p128 = new System.Drawing.Point();
            p128.X = 144;
            p128.Y = 349;
            points.Add(p128);



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
