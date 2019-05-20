using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Fatumbot
{
   
    public class REGRandomNumberGenerator : RBaseRandomProvider, IDisposable
    {
        public const long INVALID_DATASOURCE = -1;
        public const long BSS_CONNECTING = 0x0001;



        [DllImport("PsyREG.dll")]
        private extern static int PsyREGEnumerateSources();
        [DllImport("PsyREG.dll")]
        private extern static int PsyREGGetSourceCount();
        [DllImport("PsyREG.dll")]
        private extern static int PsyREGGetSource(uint n);
        [DllImport("PsyREG.dll")]
        private extern static void PsyREGClearSources();
        [DllImport("PsyREG.dll")]
        private extern static bool PsyREGOpen(int Source);
        [DllImport("PsyREG.dll")]
        private extern static bool PsyREGGetByte(int source, out byte pucBuf);
        [DllImport("PsyREG.dll")]
        private extern static void PsyREGClose(int source);
        [DllImport("PsyREG.dll")]
        private extern static void PsyREGReleaseSource(int source);
        [DllImport("PsyREG.dll")]
        private extern static bool PsyREGOpened(int source);
        [DllImport("PsyREG.dll")]
        private extern static long PsyREGGetStatus(int source);


        
        ~REGRandomNumberGenerator()
        {
            Dispose(false);
        }

        private byte[] _randomData; //random pool: array of bytes containing value on interval [0, 255]
        private int _randomDataIndex; //current position of random pool
        private int RANDOM_DATA_LENGTH = 10240; //how many random bytes do we request

        /// <summary>
        /// Quantum Random Number data source.
        /// </summary>
        private void FetchRandomData()
        {
            int iNewSources;
            int uiTotalSources;
            int Source;
            bool bResult;
            _randomData = null;
            _randomData = new byte[RANDOM_DATA_LENGTH];

            try
            {

                iNewSources = PsyREGEnumerateSources();
                if (-1 == iNewSources)
                {
                    throw new InvalidDataException("ERROR while enumerating sources");
                }
                uiTotalSources = PsyREGGetSourceCount();
                Source = PsyREGGetSource(0);
                if (INVALID_DATASOURCE == Source)
                {
                    throw new InvalidDataException("ERROR while getting source");
                }
                bResult = PsyREGOpen(Source);
                if (false == bResult)
                {
                    throw new InvalidDataException("ERROR while opening source");
                }
                while (!PsyREGOpened(Source))   /* API CALL: Wait until device is completely opened */
                {
                    if (!(PsyREGGetStatus(Source) == BSS_CONNECTING))
                    {
                        throw new InvalidDataException("ERROR while opening source");
                        break;
                    }
                }

                for (int uiRead = 0; uiRead < RANDOM_DATA_LENGTH; uiRead++)
                {
                    byte ucData = 0;
                    bResult = PsyREGGetByte(Source, out ucData);
                    if (bResult) { _randomData[uiRead] = ucData; } else { uiRead--; }
                }

                PsyREGClose(Source);    /* API CALL: Close source that has been opened */
                PsyREGReleaseSource(Source);    /* API CALL: Call to match each successful call to GetSource */
                PsyREGClearSources();
                if (_randomData.Length == RANDOM_DATA_LENGTH)
                {
                    _randomDataIndex = 0;
                    return;
                }
                else
                { _randomData = null; }
            }
            catch (Exception e) { Console.WriteLine(e.Message.ToString()); }
        }

        /// <summary>
        /// Gets random byte from cached data source. If source is not available it tries to fetch it.
        /// </summary>
        protected override byte GetRandomByte()
        {
            if (_randomData == null || _randomDataIndex == RANDOM_DATA_LENGTH)
            {
                FetchRandomData();
            }

            if (_randomData == null)
            {
                throw new InvalidDataException("Service did not return random data.");
            }

            return _randomData[_randomDataIndex++];
        }

        protected WebClient Client
        {
            get { return _client ?? (_client = new WebClient()); }
        }
        private WebClient _client;

        #region Dispose
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //clean up managed resources
                if (_client != null)
                {
                    _client.Dispose();
                    _client = null;
                }
            }

            //clean up unmanaged resources
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GC.SuppressFinalize(this);
                Dispose(true);
            }
        }

        private bool _disposed;
        #endregion
    }

    public abstract class RBaseRandomProvider : Random
    {
        protected virtual byte GetRandomByte()
        {
            return 0;
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns></returns>
        public override int Next(int minValue, int maxValue)
        { 
            if (maxValue < minValue)
            {
                throw new ArgumentOutOfRangeException("maxValue", maxValue, "maxValue must be >= minValue");
            }

            int range = maxValue - minValue;

            if (range <= 1)
            {
                throw new ArgumentOutOfRangeException("maxValue", maxValue, string.Format("maxValue must be > {0}", minValue + 1));
            }

            int rnd = Next(range);
            return rnd + minValue;
        }

        public int[,] NextCoord(int dlat, int dlon, int amount)
        {
            
            int[,] result = new int[amount,2];
            if ((dlat < 0)||(dlon < 0))
            {
                throw new ArgumentOutOfRangeException("maxValue must be >= minValue");
            }

            if ((dlat <= 1) || (dlon <= 1))
            {
                throw new ArgumentOutOfRangeException("maxValue must be > minValue");
            }
            for (int i = 0; i < amount; i++)
            {
                int rnd = Next(dlat);
                result[i, 0] = rnd;
                rnd = Next(dlon);
                result[i, 1] = rnd;
            }
            return result;
        }

        /// <summary>
        /// Returns a nonnegative random integer that is less than the specified maximum.
        /// </summary>
        public override int Next(int maxValue)
        {
            //improved method which tries to prevent modulo bias (http://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle#Modulo_bias)
            if (maxValue <= 1)
            {
                throw new ArgumentOutOfRangeException("maxValue", maxValue, "maxValue must be > 1");
            }

            //how many bits do we need to store maxValue?
            int reqBits = Convert.ToInt32(Math.Ceiling(Math.Log(maxValue, 2)));

            //how many bytes?
            int reqBytes = Convert.ToInt32(Math.Ceiling(reqBits / 8.0));

            int bitsToReset = reqBytes * 8 - reqBits;

            byte[] rnd = new byte[sizeof(int)];

            int iByteStart = reqBytes - 1;
            int randMax = Convert.ToInt32(Math.Pow(2, reqBits));

            int dbgIterationsCounter = 1;

            while (true)
            {
                NextBytes(rnd);

                //clear bits from beginning to get random number in range 0 .. 2^reqBits
                rnd[iByteStart] = (byte)(((byte)(rnd[iByteStart] << bitsToReset)) >> bitsToReset);

                //reset rest of the buffer
                for (int i = iByteStart + 1; i < rnd.Length; i++)
                {
                    rnd[i] = 0;
                }

                int n = maxValue;
                int x = BitConverter.ToInt32(rnd, 0);

                if (!(x >= (randMax - n) && x >= n))
                {
                    if (dbgIterationsCounter > 1)
                    {
                        System.Diagnostics.Debug.WriteLine("Iterations counter: {0}", dbgIterationsCounter);
                    }

                    return x % n;
                }

                dbgIterationsCounter++;
            }

            //old implementation (has modulo bias)
            //int rnd = Next();
            //return rnd % maxValue;
        }

        public override void NextBytes(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = GetRandomByte();
            }
        }

        /// <summary>
        /// Returns a nonnegative random integer.
        /// </summary>
        /// <returns></returns>
        public override int Next()
        {
            byte[] buffer = new byte[sizeof(int)];
            NextBytes(buffer);
            int rnd = BitConverter.ToInt32(buffer, 0);
            if (rnd < 0) return -rnd;
            return rnd;
        }

        /// <summary>
        /// Returns a random floating-point number between 0.0 and 1.0.
        /// </summary>
        public override double NextDouble()
        {
            double rnd = Next();
            return rnd / int.MaxValue;
        }

        protected override double Sample()
        {
            return NextDouble();
        }
    }
}
