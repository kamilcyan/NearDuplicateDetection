using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NearDuplicateDetection.Models
{
    internal class Bin
    {
        private readonly List<object> _mc;
        public Bin()
        {
            _mc = new List<object>();
        }

        public T Put<T>(T item)
        {
            if (!_mc.Contains(item))
            {
                _mc.Add(item);
            }

            return item;
        }

        internal List<(string path, List<T> hash)> GetSimilar<T>(List<T> hash, double coefficient)
        {
            return _mc.Cast<(string path, List<T> hash)>().Where(x => x.hash.Similar(hash) > coefficient).OrderByDescending(x => x.hash.Similar(hash)).ToList();
        }
    }
}
