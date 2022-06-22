using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NearDuplicateDetection.HashFunctions
{
    public class AverageHash
    {
        int size;
        int squaresize => size * size;

        public AverageHash(int accuracy = 16)
        {
            size = accuracy;
        }

        internal ConcurrentDictionary<string, List<(string otherFile, double match)>> DoPHash(string source, string directory, string extension, bool nameDuplicat = false)
        {
            var files = Directory.GetFiles(directory).Where(x => x.EndsWith(extension)).ToList();

            return DoPHash(source, files, nameDuplicat);
        }

        internal ConcurrentDictionary<string, List<(string otherFile, double match)>> DoPHash(string source, List<string> fileList, bool nameDuplicat)
        {
            var hashes = new ConcurrentDictionary<string, byte[,]>();
            List<string> results = new List<string>();
            var tasks = new List<Task>();
            foreach (var item in fileList)
            {
                var t = new Task(() =>
                {
                    hashes.TryAdd(item, CalculatePHash(item));
                });
                tasks.Add(t);
                t.Start();
            }

            var tt = new Task(() =>
            {
                hashes.TryAdd(source, CalculatePHash(source));
            });
            tasks.Add(tt);
            tt.Start();
            Task.WaitAll(tasks.ToArray());

            var result = new ConcurrentDictionary<string, List<(string otherFile, double match)>>();

            result = CalculateMatchRate(source, fileList, hashes, result, nameDuplicat);

            results = WriteResult(result);
            return result;
        }

        private byte[,] CalculatePHash(string item)
        {
            var result = new byte[size, size];
            GetImageBytes(item, result, size, size);
            var sum = 0;
            foreach (var x in result)
            {
                sum += x;
            }
            var avg = sum / (squaresize);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if ((int)result[x, y] > avg)
                    {
                        result[x, y] = 1;
                    }
                    else
                    {
                        result[x, y] = 0;
                    }
                }
            }
            return result;
        }

        private void GetImageBytes(string item, byte[,] result, int sizeX, int sizeY)
        {
            System.Drawing.Image img = null;
            Bitmap bitmap = null;
            Graphics graphics = null;
            try
            {
                img = System.Drawing.Image.FromFile(item);

                bitmap = new Bitmap(sizeX, sizeY);
                graphics = Graphics.FromImage((System.Drawing.Image)bitmap);
                graphics.InterpolationMode = InterpolationMode.Low;
                // Draw image with new width and height  
                graphics.DrawImage(img, 0, 0, sizeX, sizeY);
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        Color c = bitmap.GetPixel(x, y);

                        int r = c.R;
                        int g = c.G;
                        int b = c.B;
                        int a = (r + g + b) / 3;
                        result[x, y] = (byte)a;
                    }
                }

                //(bitmap as Image).Save("C:\\fraps\\test.jpg");

            }
            finally
            {
                graphics.Dispose();
                bitmap.Dispose();
                img.Dispose();
            }
        }

        private ConcurrentDictionary<string, List<(string otherFile, double match)>> CalculateMatchRate(string source, List<string> filePaths, ConcurrentDictionary<string, byte[,]> hashes, ConcurrentDictionary<string, List<(string otherFile, double match)>> result, bool nameDuplicat)
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

                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        if (inputArray[i, j] == secondArray[i, j])
                        {
                            matchCount++;
                        }

                    }
                }

                //for (int x = 0; x < secondArray.Length; x++)
                //{
                //    if (inputArray[x] == secondArray[x])
                //    {
                //    }
                //}
                matches.Add((secondItem, (double)matchCount / (double)(secondArray.Length)));
            }
            result.TryAdd(source, matches);
            return result;
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
    }
}
