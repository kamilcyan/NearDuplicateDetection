using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NearDuplicateDetection.Models
{
    internal class BinContainer
    {
        private readonly Dictionary<string, Bin> _bins;
        public BinContainer()
        {
            _bins = new Dictionary<string, Bin>();
        }

        void Add<T>(string path, List<T> hash)
        {
            var reduced = hash.Reduce();
            var name = reduced.ToName();
            if (!_bins.ContainsKey(name))
            {
                _bins[name] = new Bin();
            }
            var bin = _bins[name];
            bin.Put((path, hash));
        }

        List<(string path, List<T> hash)> GetSimilar<T>(List<T> hash, double coefficient = 0.9)
        {
            var reduced = hash.Reduce();
            var name = reduced.ToName();
            if (!_bins.ContainsKey(name))
            {
                throw new Exception("No bins similar to this image");
            }

            var bin = _bins[name];
            return bin.GetSimilar(hash, coefficient);
        }
    }
}
