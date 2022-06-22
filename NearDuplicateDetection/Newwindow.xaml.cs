using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace NearDuplicateDetection
{
    /// <summary>
    /// Interaction logic for Newwindow.xaml
    /// </summary>
    public partial class Newwindow : Window
    {
        public Newwindow()
        {
            InitializeComponent();
        }

        public void ShowResults(List<string> results, List<string> results2)
        {
            ResultList2.TextWrapping = TextWrapping.Wrap;
            foreach (var r in results)
            {
                ResultList2.Text += r;
            }
            //int iter = 1;
            //foreach(var p in points)
            //{
            //    ResultList2.Text += "System.Drawing.Point p" + iter + " = new System.Drawing.Point();\n";
            //    ResultList2.Text += "p" + iter + ".X = " + p.X + ";\n";
            //    ResultList2.Text += "p" + iter + ".Y = " + p.Y + ";\n";
            //    ResultList2.Text += "points.Add(p" + iter + ");\n";
            //    iter++;
            //}

            foreach (var r in results2)
            {
                ResultList2.Text += r;
            }
        }
    }
}
