using DeltaEncoding.RSync.Shared.Serialization;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaEncoding.RSync.Test
{
    public class DC1Test
    {
        [Fact]
        public void Test1()
        {
            var stream1 = new FileStream("./samples/d1.dll", FileMode.Open);
            var dictionary = new Dictionary<byte, List<long>>();
            var pos = 0;
            foreach(var kv in dictionary)
            {
                var x  = Enumerable.Range(0,kv.Value.Count).Select(v => (double)v).ToArray();
                var y = kv.Value.Select(v => (double)v).ToArray();
                for(var i=1; i<y.Length; i++)
                    y[i] -= y[i - 1];
                var parameters = Fit.Line(x, y);
                var a = parameters.A;
                var b = parameters.B;
                var p = 0;
                foreach(var v in y)
                {
                    var dif = a+b*p-v;
                    p++;
                    int z = 0;
                }
            }
            
        }
    }
}
