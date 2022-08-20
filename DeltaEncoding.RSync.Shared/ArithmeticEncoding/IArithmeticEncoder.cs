using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaEncoding.RSync.Shared.ArithmeticEncoding
{
    public interface IArithmeticEncoder
    {
        public void Push(byte[] bytes, int start, int length);
    }
}
