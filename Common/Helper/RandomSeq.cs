using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    /// <summary>
    /// could gerenate random sequences
    /// - random string of [0-9,A-Z,a-z] chars
    /// - random byte[] array
    /// </summary>
    public class RandomSeq
    {
        private readonly Random m_r = new Random();
        private static string m_sValues = "01234567890ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public RandomSeq()
        { }

        public string GenString(int nLength)
        {
            var sb = new StringBuilder();

            for (int n = 0; n < nLength; n++)
            {
                int nV = m_r.Next(m_sValues.Length);
                if (nV >= m_sValues.Length)
                    nV = m_sValues.Length - 1;

                sb.Append(m_sValues.Substring(nV, 1));
            }

            return sb.ToString();
        }

        public string GenNum(int nLength)
        {
            var sb = new StringBuilder();

            for (int n = 0; n < nLength; n++)
            {
                int nV = m_r.Next(10);
                if (nV >= 10)
                    nV = 9;

                sb.Append(nV.ToString());
            }

            return sb.ToString();
        }

        public byte[] GenArray(int nLength)
        {
            byte[] a = new byte[nLength];

            for (int n = 0; n < nLength; n++)
            {
                int nV = m_r.Next(256);
                if (nV >= 255)
                    nV = 255;

                a[n] = (byte)nV;
            }

            return a;
        }
    }
}
